using System.Security.Claims;
using System.Text;
using FiapCloudGames.UsersApi.Data;
using FiapCloudGames.UsersApi.Domain;
using FiapCloudGames.UsersApi.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);
var jwtSecret = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
    throw new InvalidOperationException("Jwt:SecretKey deve ter pelo menos 32 caracteres.");

builder.Services.AddDbContext<UsersDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("UsersDatabase")));
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddMassTransit(bus => bus.UsingRabbitMq((context, cfg) =>
{
    cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", host =>
    {
        host.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
        host.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
    });
    cfg.ConfigureEndpoints(context);
}));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        RoleClaimType = ClaimTypes.Role
    };
});
builder.Services.AddAuthorization(options =>
    options.AddPolicy("Administrator", policy => policy.RequireRole(UserRole.Administrator.ToString())));
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "FCG Users API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = Array.Empty<string>()
    });
});

var app = builder.Build();
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpMetrics();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapMetrics();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
    await db.Database.EnsureCreatedAsync();
    await scope.ServiceProvider.GetRequiredService<UserService>().SeedAdministratorAsync();
}

var users = app.MapGroup("/api/users").WithTags("Users");
users.MapPost("/", async (CreateUserRequest request, UserService service, CancellationToken ct) =>
{
    var user = await service.CreateAsync(request.Name, request.Email, request.Password, cancellationToken: ct);
    return Results.Created($"/api/users/{user.Id}", user.ToResponse());
}).AllowAnonymous();

users.MapPost("/login", async (LoginRequest request, UserService service, JwtTokenService tokens, CancellationToken ct) =>
{
    var user = await service.AuthenticateAsync(request.Email, request.Password, ct);
    return Results.Ok(new LoginResponse(tokens.Create(user), user.ToResponse()));
}).AllowAnonymous();

users.MapGet("/me", async (ClaimsPrincipal principal, UserService service, CancellationToken ct) =>
{
    var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!Guid.TryParse(value, out var id)) return Results.Unauthorized();
    var user = await service.FindAsync(id, ct);
    return user is null ? Results.NotFound() : Results.Ok(user.ToResponse());
}).RequireAuthorization();

users.MapGet("/", async (UserService service, CancellationToken ct) =>
    Results.Ok((await service.ListAsync(ct)).Select(x => x.ToResponse())))
    .RequireAuthorization("Administrator");

app.Run();

public partial class Program;
public sealed record CreateUserRequest(string Name, string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record LoginResponse(string Token, UserResponse User);
public sealed record UserResponse(Guid Id, string Name, string Email, string Role, DateTime CreatedAtUtc);

internal static class ResponseMapping
{
    public static UserResponse ToResponse(this User user) =>
        new(user.Id, user.Name, user.Email, user.Role.ToString(), user.CreatedAtUtc);
}

internal sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (ConflictException ex) { await WriteAsync(context, 409, ex.Message); }
        catch (UnauthorizedAccessException ex) { await WriteAsync(context, 401, ex.Message); }
        catch (ArgumentException ex) { await WriteAsync(context, 400, ex.Message); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled request error");
            await WriteAsync(context, 500, "Erro interno.");
        }
    }

    private static async Task WriteAsync(HttpContext context, int status, string message)
    {
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { error = message });
    }
}
