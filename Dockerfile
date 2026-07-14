FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/FiapCloudGames.UsersApi/FiapCloudGames.UsersApi.csproj src/FiapCloudGames.UsersApi/
RUN dotnet restore src/FiapCloudGames.UsersApi/FiapCloudGames.UsersApi.csproj
COPY src/FiapCloudGames.UsersApi/ src/FiapCloudGames.UsersApi/
RUN dotnet publish src/FiapCloudGames.UsersApi/FiapCloudGames.UsersApi.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN adduser --disabled-password --gecos "" --home /app appuser && mkdir -p /data && chown -R appuser:appuser /app /data
USER appuser
COPY --from=build --chown=appuser:appuser /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "FiapCloudGames.UsersApi.dll"]
