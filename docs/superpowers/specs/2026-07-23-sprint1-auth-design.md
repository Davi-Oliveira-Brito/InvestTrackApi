# Sprint 1 — Auth (User + EF Core + JWT) — Design

## Contexto

Primeiro sprint funcional do InvestTrack API (ver `ROADMAP.md`). Objetivo: entidade `User`, EF Core configurado contra o Supabase, e endpoints de registro/login com JWT. Front-end (InvestTrackWeb, repo separado) está fora de escopo desta sessão — este documento cobre apenas o backend.

## Arquitetura

Segue a regra de dependência já documentada no README (`Domain ← Application ← Infrastructure ← Api`).

```
Domain
  └─ User (entidade)

Application
  └─ Interfaces: IUserRepository, IPasswordHasher, IJwtTokenGenerator
  └─ DTOs: RegisterRequest, LoginRequest, AuthResponse
  └─ IAuthService / AuthService: RegisterAsync, LoginAsync

Infrastructure
  └─ AppDbContext (EF Core, Npgsql)
  └─ UserRepository (implementa IUserRepository)
  └─ BCryptPasswordHasher (implementa IPasswordHasher)
  └─ JwtTokenGenerator (implementa IJwtTokenGenerator)
  └─ Migrations/

Api
  └─ AuthController: POST /api/auth/register, POST /api/auth/login
  └─ Program.cs: DI, EF Core, CORS, JWT Bearer middleware
```

Domain não referencia nenhuma outra camada. Application só conhece interfaces (não sabe que a implementação é Postgres/BCrypt). Infrastructure implementa tudo. Api monta as peças via DI.

## Entidade `User` (Domain)

Campos: `Id (Guid)`, `Nome`, `Email`, `PasswordHash`, `CreatedAt`.

Construtor privado + factory method `User.Criar(nome, email, passwordHash)` que valida nome/email não vazios — regra de negócio vive no Domain, não no controller.

## Padrão de Application

Serviços simples com interface (não CQRS/MediatR) — `AuthService` chamado diretamente pelo `AuthController`. Escolhido por ser direto, fácil de testar e adequado ao tamanho atual do projeto.

## Fluxo de registro

`POST /api/auth/register` com `RegisterRequest { Nome, Email, Password }`:

1. Validação via Data Annotations: `[Required]`, `[EmailAddress]`, senha mínimo 8 caracteres com pelo menos 1 letra e 1 número (regex).
2. `AuthService.RegisterAsync`:
   - Verifica duplicidade de email via `IUserRepository` (checagem de aplicação — ver seção de concorrência abaixo).
   - Gera hash da senha via `BCryptPasswordHasher`.
   - Cria `User` via factory method e persiste.
3. Retorna `AuthResponse { Token, ExpiraEm }` — usuário já fica autenticado após registrar, sem precisar de login separado.

## Fluxo de login

`POST /api/auth/login` com `LoginRequest { Email, Password }`:

1. `AuthService.LoginAsync` busca usuário por email.
2. Verifica senha com `BCrypt.Verify` (nunca decripta).
3. Se válido, gera JWT via `JwtTokenGenerator`: claims `sub` (UserId), `email`, `name`; expiração 2h.
4. Retorna `AuthResponse { Token, ExpiraEm }`.

## Concorrência: unicidade de email

A checagem de duplicidade em `AuthService.RegisterAsync` não é suficiente sozinha — duas requisições simultâneas de registro com o mesmo email podem passar pela checagem antes de qualquer uma persistir (race condition). Rede de segurança real: constraint `UNIQUE` no banco via `HasIndex(u => u.Email).IsUnique()` no `OnModelCreating` do `AppDbContext`, entrando na migration inicial. Se a constraint disparar (ex: corrida vencida), a camada de erro trata como o mesmo 409 do caso de duplicidade normal.

## CORS

Necessário porque o front (Vercel) e a API (Render) vivem em domínios diferentes — sem CORS, o navegador bloqueia as chamadas do Next.js assim que o Sprint 1 do front conectar os dois.

- `Program.cs`: `AddCors` com policy `AllowFrontend`, origens lidas de configuração (`Cors:AllowedOrigins`, array em `appsettings.json`/`appsettings.Development.json`), não hardcoded — permite atualizar sem recompilar.
- `appsettings.Development.json`: inclui `http://localhost:3000`.
- `appsettings.json` (produção): placeholder documentado para o domínio Vercel definitivo, a ser preenchido quando o front tiver domínio fixo (pode ser sobrescrito via env var no Render sem precisar de novo deploy de código).
- `app.UseCors("AllowFrontend")` deve vir antes de `app.UseAuthentication()`.

## Erros

- Email duplicado no registro → `409 Conflict`, mensagem clara ("email já cadastrado").
- Credenciais inválidas no login → `401 Unauthorized`, mensagem genérica ("email ou senha inválidos") — não revela qual campo errou, evita enumeration attack.
- Validação de formato (email/senha) → `400 Bad Request` via `ProblemDetails` padrão do ASP.NET (automático com `[ApiController]`).

## Program.cs — mudanças

- `AddDbContext<AppDbContext>` com Npgsql, lendo `ConnectionStrings:DefaultConnection`.
- `AddCors(...)` (ver seção CORS).
- `AddAuthentication().AddJwtBearer(...)` lendo secret de `Jwt:Secret` (config) — **precisa estar em user-secrets local (`dotnet user-secrets set "Jwt:Secret" "..."`) antes de rodar, senão a inicialização quebra**.
- `AddAuthorization()`.
- Registro dos serviços de DI (`IUserRepository`, `IPasswordHasher`, `IJwtTokenGenerator`, `IAuthService`).
- Ordem de middleware: `UseCors` → `UseAuthentication` → `UseAuthorization`.

## Pacotes NuGet novos

- Infrastructure: `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`, `BCrypt.Net-Next`.
- Api: `Microsoft.AspNetCore.Authentication.JwtBearer`.

## Migration

Migration inicial `InitialCreate` (tabela `Users`, incluindo o índice único de email), aplicada contra o Supabase configurado nos user-secrets locais. Validar que a tabela é criada de fato.

## Testes

Testes unitários de `AuthService` usando mocks de `IUserRepository`/`IPasswordHasher`/`IJwtTokenGenerator` (sem banco real):
- Registro com email duplicado é rejeitado.
- Login com senha errada é rejeitado.
- Happy path de registro e de login geram token.

## Pendências explícitas (fora de escopo desta sessão)

- **Refresh token**: adiado deliberadamente. Sprint 1 usa só access token de 2h. Marcar no `ROADMAP.md` como pendência para sprint futuro.
- **Front-end**: repo separado (InvestTrackWeb), não coberto aqui.
- **Deploy real no Render**: configurar `Jwt:Secret` e `Cors:AllowedOrigins` (domínio Vercel definitivo) como env vars no painel do Render é passo manual fora do alcance do Claude Code — sinalizar quando chegar nesse ponto.
