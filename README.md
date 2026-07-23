# InvestTrack API

Plataforma de análise de portfólio de investimentos. API responsável por gerenciar carteiras, calcular métricas de rentabilidade e risco (volatilidade, Sharpe Ratio, drawdown) e comparar performance com benchmarks (CDI/Ibovespa).

- **API em produção:** [investtrackapi.onrender.com](https://investtrackapi.onrender.com)
- **Documentação (Scalar):** [investtrackapi.onrender.com/scalar/v1](https://investtrackapi.onrender.com/scalar/v1)
- **Front-end:** [InvestTrackWeb](https://github.com/Davi-Oliveira-Brito/InvestTrackWeb)

## Stack

- **Linguagem:** C# / .NET 10
- **Arquitetura:** Clean Architecture
- **Banco de dados:** PostgreSQL (hospedado no [Supabase](https://supabase.com))
- **Documentação da API:** Scalar (OpenAPI 3.1)
- **Deploy:** Docker + [Render](https://render.com)
- **CI/CD:** GitHub Actions *(em breve)*

## Arquitetura

O projeto segue Clean Architecture, dividido em 4 camadas com regra de dependência estrita — o `Domain` não depende de nenhuma outra camada:

```
InvestTrack.Domain           → Entidades e regras de negócio puras
InvestTrack.Application      → Casos de uso, DTOs, interfaces
InvestTrack.Infrastructure   → EF Core, Postgres, integrações externas
InvestTrack.Api              → Endpoints HTTP, autenticação, middlewares
```

```
Api  →  Application, Infrastructure
Infrastructure  →  Application, Domain
Application  →  Domain
Domain  →  (nenhuma dependência)
```

## Como rodar localmente

### Pré-requisitos
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Uma instância PostgreSQL (local via Docker ou uma instância no Supabase)

### Passos

```bash
# clonar o repositório
git clone https://github.com/Davi-Oliveira-Brito/InvestTrackApi.git
cd InvestTrack

# configurar a connection string (não vai para o Git)
cd InvestTrack.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "sua-connection-string-aqui"
cd ..

# rodar a API
dotnet run --project InvestTrack.Api
```

A API sobe por padrão em `http://localhost:5158`. Documentação interativa disponível em `http://localhost:5158/scalar/v1`.

## Roadmap

O desenvolvimento está sendo feito em sprints. Acompanhe o progresso em [ROADMAP.md](./ROADMAP.md).

## Deploy

- **API:** deploy automático no Render a cada push na branch `main`, via Dockerfile.
- **Banco:** PostgreSQL gerenciado no Supabase.