# FCG UsersAPI

Microsservico de cadastro, autenticacao JWT e autorizacao de usuarios. Ao concluir um cadastro, publica `UserCreatedEvent` no RabbitMQ.

## Executar localmente

Inicie um RabbitMQ em `localhost:5672` e execute:

```powershell
$env:Jwt__SecretKey='development-only-secret-key-change-me-123456789'
$env:RabbitMq__Password='guest'
dotnet run --project src/FiapCloudGames.UsersApi
```

Swagger: `http://localhost:5101/swagger`. Health check: `/health`. Métricas Prometheus: `/metrics`.

## Variaveis de ambiente

| Variavel | Finalidade |
| --- | --- |
| `ConnectionStrings__UsersDatabase` | Connection string SQLite |
| `Jwt__Issuer` / `Jwt__Audience` | Emissor e audiencia do JWT |
| `Jwt__SecretKey` | Chave de assinatura, minimo de 32 caracteres |
| `RabbitMq__Host` | Host do RabbitMQ |
| `RabbitMq__Username` / `RabbitMq__Password` | Credenciais do broker |
| `ADMIN_EMAIL` / `ADMIN_PASSWORD` | Administrador inicial opcional |

## Evento publicado

```text
FiapCloudGames.Contracts.UserCreatedEvent
UserId: Guid; Name: string; Email: string; OccurredAtUtc: DateTime
```

## Docker e Kubernetes

```powershell
docker build -t fiapcloudgames/users-api:latest .
kubectl apply -f k8s/users-api.yaml
```

Os valores `CHANGE-ME` no Secret representam segredos de ambiente. O manifesto espera o RabbitMQ acessivel pelo Service `rabbitmq`.

## Testes

```powershell
dotnet test tests/FiapCloudGames.UsersApi.Tests
```
