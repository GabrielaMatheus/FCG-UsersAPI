# FIAP Cloud Games - UsersAPI - Fase 4

Microsservico .NET 8 de cadastro, autenticacao JWT e autorizacao. Ao cadastrar um usuario, publica `UserCreatedEvent` no RabbitMQ.

## Persistencia e execucao

- Producao: Azure Database for PostgreSQL Flexible Server, permitindo multiplas replicas da API.
- Desenvolvimento: SQLite, selecionado por `Database__Provider=Sqlite`.
- Producao: `Database__Provider=Postgres`.

As connection strings, o segredo JWT, a senha do administrador e as credenciais do RabbitMQ sao obrigatorios em tempo de execucao. Nenhum deles fica no codigo ou nos YAMLs. O repositorio de orquestracao sincroniza o Azure Key Vault com o Secret `fcg-runtime` do Kubernetes.

## Endpoints

| Metodo | Rota | Acesso |
| --- | --- | --- |
| POST | `/api/users` | Publico |
| POST | `/api/users/login` | Publico |
| GET | `/api/users/me` | JWT |
| GET | `/api/users` | Administrador |

Health check: `/health`. Metricas Prometheus: `/metrics`. Swagger: `/swagger`.

## Testes

```powershell
dotnet test tests/FiapCloudGames.UsersApi.Tests/FiapCloudGames.UsersApi.Tests.csproj
```

## CI/CD

`.github/workflows/ci-cd.yml` executa build, testes, build da imagem, Trivy, push versionado para o Azure Container Registry e rolling update no AKS. O login no Azure usa OIDC, sem senha persistente.

Configure o GitHub Environment `production` com estas variables:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `AZURE_RESOURCE_GROUP`
- `AKS_CLUSTER_NAME`
- `ACR_NAME`
- `ACR_LOGIN_SERVER`
- `ACR_REPOSITORY=fcg-users-api`

O Terraform, os manifestos e o deploy inicial estao no repositorio `FCG-Orchestration`.
