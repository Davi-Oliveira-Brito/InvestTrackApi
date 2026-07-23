# Sprint 1 — Auth (User + EF Core + JWT) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the entidade `User`, EF Core wiring against Supabase, and `POST /api/auth/register` + `POST /api/auth/login` endpoints returning a JWT, following Clean Architecture and the spec at `docs/superpowers/specs/2026-07-23-sprint1-auth-design.md`.

**Architecture:** `Domain` holds the `User` entity with a validating factory method. `Application` defines `IUserRepository`/`IPasswordHasher`/`IJwtTokenGenerator` interfaces and an `AuthService` that orchestrates registration/login using only those interfaces. `Infrastructure` implements the interfaces (EF Core + Npgsql, BCrypt, JWT generation) and exposes an `AddInfrastructure` DI extension. `Api` wires everything in `Program.cs`, exposes `AuthController`, and adds CORS + JWT Bearer authentication middleware.

**Tech Stack:** .NET 10, ASP.NET Core, EF Core + Npgsql (PostgreSQL/Supabase), BCrypt.Net-Next, System.IdentityModel.Tokens.Jwt, xUnit + Moq for tests.

---

## Before you start

Run every command from the repo root (`C:/Users/davio/source/repos/InvestTrack`) unless a step says otherwise. The solution file is `InvestTrack.slnx` (new XML solution format — `dotnet sln` works with it the same as a `.sln`).

Confirmed already true in this environment (no need to re-check):
- .NET 10 SDK (`10.0.201`) is installed.
- `dotnet-ef` global tool (`10.0.9`) is installed.
- `InvestTrack.Api` already has `ConnectionStrings:DefaultConnection` set in user-secrets, pointing at the real Supabase Postgres instance.

---

### Task 1: Remove template leftovers

The solution was scaffolded from default templates. Before adding real code, remove the placeholder files so nothing shadows the new auth code.

**Files:**
- Delete: `InvestTrack.Api/Controllers/WeatherForecastController.cs`
- Delete: `InvestTrack.Api/WeatherForecast.cs`
- Delete: `InvestTrack.Domain/Class1.cs`
- Delete: `InvestTrack.Application/Class1.cs`
- Delete: `InvestTrack.Infrastructure/Class1.cs`

- [ ] **Step 1: Delete the five template files**

```bash
rm InvestTrack.Api/Controllers/WeatherForecastController.cs
rm InvestTrack.Api/WeatherForecast.cs
rm InvestTrack.Domain/Class1.cs
rm InvestTrack.Application/Class1.cs
rm InvestTrack.Infrastructure/Class1.cs
```

- [ ] **Step 2: Verify the solution still builds**

Run: `dotnet build InvestTrack.slnx`
Expected: `Build succeeded.` (each project now has zero `.cs` files except `Program.cs` in Api — that's fine, class libraries build with no source files).

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "chore: remove template leftovers (WeatherForecast, Class1)"
```

---

### Task 2: Domain — `User` entity (TDD)

**Files:**
- Create: `InvestTrack.Domain.Tests/InvestTrack.Domain.Tests.csproj` (generated)
- Create: `InvestTrack.Domain.Tests/UserTests.cs`
- Create: `InvestTrack.Domain/Entities/User.cs`

- [ ] **Step 1: Scaffold the Domain test project**

```bash
dotnet new xunit -n InvestTrack.Domain.Tests -o InvestTrack.Domain.Tests
dotnet sln InvestTrack.slnx add InvestTrack.Domain.Tests/InvestTrack.Domain.Tests.csproj
dotnet add InvestTrack.Domain.Tests/InvestTrack.Domain.Tests.csproj reference InvestTrack.Domain/InvestTrack.Domain.csproj
rm InvestTrack.Domain.Tests/UnitTest1.cs
```

- [ ] **Step 2: Write the failing tests**

Create `InvestTrack.Domain.Tests/UserTests.cs`:

```csharp
using InvestTrack.Domain.Entities;
using Xunit;

namespace InvestTrack.Domain.Tests
{
    public class UserTests
    {
        [Fact]
        public void Criar_ComDadosValidos_CriaUsuarioComSucesso()
        {
            var user = User.Criar("Davi Brito", "davi@teste.com", "hash123");

            Assert.NotEqual(Guid.Empty, user.Id);
            Assert.Equal("Davi Brito", user.Nome);
            Assert.Equal("davi@teste.com", user.Email);
            Assert.Equal("hash123", user.PasswordHash);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Criar_ComNomeInvalido_LancaArgumentException(string? nomeInvalido)
        {
            Assert.Throws<ArgumentException>(() => User.Criar(nomeInvalido!, "davi@teste.com", "hash123"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Criar_ComEmailInvalido_LancaArgumentException(string? emailInvalido)
        {
            Assert.Throws<ArgumentException>(() => User.Criar("Davi Brito", emailInvalido!, "hash123"));
        }
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test InvestTrack.Domain.Tests/InvestTrack.Domain.Tests.csproj`
Expected: build error — `User` and `InvestTrack.Domain.Entities` don't exist yet.

- [ ] **Step 4: Implement the `User` entity**

Create `InvestTrack.Domain/Entities/User.cs`:

```csharp
namespace InvestTrack.Domain.Entities
{
    public class User
    {
        public Guid Id { get; private set; }
        public string Nome { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private User(Guid id, string nome, string email, string passwordHash, DateTime createdAt)
        {
            Id = id;
            Nome = nome;
            Email = email;
            PasswordHash = passwordHash;
            CreatedAt = createdAt;
        }

        public static User Criar(string nome, string email, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("Nome não pode ser vazio.", nameof(nome));

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email não pode ser vazio.", nameof(email));

            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("PasswordHash não pode ser vazio.", nameof(passwordHash));

            return new User(Guid.NewGuid(), nome, email, passwordHash, DateTime.UtcNow);
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test InvestTrack.Domain.Tests/InvestTrack.Domain.Tests.csproj`
Expected: `Passed! - Failed: 0, Passed: 7` (1 + 3 + 3 from the two `[Theory]` cases)

- [ ] **Step 6: Commit**

```bash
git add InvestTrack.Domain InvestTrack.Domain.Tests InvestTrack.slnx
git commit -m "feat: add User entity with validating factory method"
```

---

### Task 3: Application — DTOs, interfaces, exceptions

**Files:**
- Create: `InvestTrack.Application/Dtos/RegisterRequest.cs`
- Create: `InvestTrack.Application/Dtos/LoginRequest.cs`
- Create: `InvestTrack.Application/Dtos/AuthResponse.cs`
- Create: `InvestTrack.Application/Interfaces/IUserRepository.cs`
- Create: `InvestTrack.Application/Interfaces/IPasswordHasher.cs`
- Create: `InvestTrack.Application/Interfaces/IJwtTokenGenerator.cs`
- Create: `InvestTrack.Application/Interfaces/IAuthService.cs`
- Create: `InvestTrack.Application/Exceptions/EmailJaCadastradoException.cs`
- Create: `InvestTrack.Application/Exceptions/CredenciaisInvalidasException.cs`

These are plain data holders/interfaces/exceptions with no branching logic — no dedicated tests (they're exercised indirectly by the `AuthService` tests in Task 4).

- [ ] **Step 1: Create the DTOs**

Create `InvestTrack.Application/Dtos/RegisterRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace InvestTrack.Application.Dtos
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Nome é obrigatório.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Email em formato inválido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Senha é obrigatória.")]
        [MinLength(8, ErrorMessage = "Senha deve ter no mínimo 8 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Senha deve conter ao menos uma letra e um número.")]
        public string Password { get; set; } = string.Empty;
    }
}
```

Create `InvestTrack.Application/Dtos/LoginRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace InvestTrack.Application.Dtos
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Email em formato inválido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Senha é obrigatória.")]
        public string Password { get; set; } = string.Empty;
    }
}
```

Create `InvestTrack.Application/Dtos/AuthResponse.cs`:

```csharp
namespace InvestTrack.Application.Dtos
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiraEm { get; set; }
    }
}
```

- [ ] **Step 2: Create the interfaces**

Create `InvestTrack.Application/Interfaces/IUserRepository.cs`:

```csharp
using InvestTrack.Domain.Entities;

namespace InvestTrack.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> ObterPorEmailAsync(string email);
        Task AdicionarAsync(User user);
    }
}
```

Create `InvestTrack.Application/Interfaces/IPasswordHasher.cs`:

```csharp
namespace InvestTrack.Application.Interfaces
{
    public interface IPasswordHasher
    {
        string Hash(string senha);
        bool Verify(string senha, string hash);
    }
}
```

Create `InvestTrack.Application/Interfaces/IJwtTokenGenerator.cs`:

```csharp
namespace InvestTrack.Application.Interfaces
{
    public interface IJwtTokenGenerator
    {
        (string Token, DateTime ExpiraEm) GerarToken(Guid userId, string email, string nome);
    }
}
```

Create `InvestTrack.Application/Interfaces/IAuthService.cs`:

```csharp
using InvestTrack.Application.Dtos;

namespace InvestTrack.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
    }
}
```

- [ ] **Step 3: Create the exceptions**

Create `InvestTrack.Application/Exceptions/EmailJaCadastradoException.cs`:

```csharp
namespace InvestTrack.Application.Exceptions
{
    public class EmailJaCadastradoException : Exception
    {
        public EmailJaCadastradoException(string email)
            : base($"O email '{email}' já está cadastrado.")
        {
        }
    }
}
```

Create `InvestTrack.Application/Exceptions/CredenciaisInvalidasException.cs`:

```csharp
namespace InvestTrack.Application.Exceptions
{
    public class CredenciaisInvalidasException : Exception
    {
        public CredenciaisInvalidasException()
            : base("Email ou senha inválidos.")
        {
        }
    }
}
```

- [ ] **Step 4: Build to verify it compiles**

Run: `dotnet build InvestTrack.Application/InvestTrack.Application.csproj`
Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add InvestTrack.Application
git commit -m "feat: add auth DTOs, interfaces and exceptions"
```

---

### Task 4: Application — `AuthService` (TDD)

**Files:**
- Create: `InvestTrack.Application.Tests/InvestTrack.Application.Tests.csproj` (generated)
- Create: `InvestTrack.Application.Tests/AuthServiceTests.cs`
- Create: `InvestTrack.Application/Services/AuthService.cs`

- [ ] **Step 1: Scaffold the Application test project with Moq**

```bash
dotnet new xunit -n InvestTrack.Application.Tests -o InvestTrack.Application.Tests
dotnet sln InvestTrack.slnx add InvestTrack.Application.Tests/InvestTrack.Application.Tests.csproj
dotnet add InvestTrack.Application.Tests/InvestTrack.Application.Tests.csproj reference InvestTrack.Application/InvestTrack.Application.csproj
dotnet add InvestTrack.Application.Tests/InvestTrack.Application.Tests.csproj package Moq
rm InvestTrack.Application.Tests/UnitTest1.cs
```

- [ ] **Step 2: Write the failing tests**

Create `InvestTrack.Application.Tests/AuthServiceTests.cs`:

```csharp
using InvestTrack.Application.Dtos;
using InvestTrack.Application.Exceptions;
using InvestTrack.Application.Interfaces;
using InvestTrack.Application.Services;
using InvestTrack.Domain.Entities;
using Moq;
using Xunit;

namespace InvestTrack.Application.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock = new();
        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _sut = new AuthService(
                _userRepositoryMock.Object,
                _passwordHasherMock.Object,
                _jwtTokenGeneratorMock.Object);
        }

        [Fact]
        public async Task RegisterAsync_ComEmailJaCadastrado_LancaEmailJaCadastradoException()
        {
            var request = new RegisterRequest { Nome = "Davi", Email = "davi@teste.com", Password = "senha123" };
            _userRepositoryMock
                .Setup(r => r.ObterPorEmailAsync(request.Email))
                .ReturnsAsync(User.Criar("Existente", request.Email, "hash-existente"));

            await Assert.ThrowsAsync<EmailJaCadastradoException>(() => _sut.RegisterAsync(request));
        }

        [Fact]
        public async Task RegisterAsync_ComDadosValidos_RetornaTokenEPersisteUsuario()
        {
            var request = new RegisterRequest { Nome = "Davi", Email = "davi@teste.com", Password = "senha123" };
            _userRepositoryMock.Setup(r => r.ObterPorEmailAsync(request.Email)).ReturnsAsync((User?)null);
            _passwordHasherMock.Setup(h => h.Hash(request.Password)).Returns("hash-gerado");
            var expiraEm = DateTime.UtcNow.AddHours(2);
            _jwtTokenGeneratorMock
                .Setup(j => j.GerarToken(It.IsAny<Guid>(), request.Email, request.Nome))
                .Returns(("token-gerado", expiraEm));

            var resultado = await _sut.RegisterAsync(request);

            Assert.Equal("token-gerado", resultado.Token);
            Assert.Equal(expiraEm, resultado.ExpiraEm);
            _userRepositoryMock.Verify(
                r => r.AdicionarAsync(It.Is<User>(u => u.Email == request.Email && u.PasswordHash == "hash-gerado")),
                Times.Once);
        }

        [Fact]
        public async Task LoginAsync_ComEmailInexistente_LancaCredenciaisInvalidasException()
        {
            var request = new LoginRequest { Email = "naoexiste@teste.com", Password = "qualquer123" };
            _userRepositoryMock.Setup(r => r.ObterPorEmailAsync(request.Email)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<CredenciaisInvalidasException>(() => _sut.LoginAsync(request));
        }

        [Fact]
        public async Task LoginAsync_ComSenhaErrada_LancaCredenciaisInvalidasException()
        {
            var request = new LoginRequest { Email = "davi@teste.com", Password = "senhaErrada" };
            var usuario = User.Criar("Davi", request.Email, "hash-correto");
            _userRepositoryMock.Setup(r => r.ObterPorEmailAsync(request.Email)).ReturnsAsync(usuario);
            _passwordHasherMock.Setup(h => h.Verify(request.Password, usuario.PasswordHash)).Returns(false);

            await Assert.ThrowsAsync<CredenciaisInvalidasException>(() => _sut.LoginAsync(request));
        }

        [Fact]
        public async Task LoginAsync_ComCredenciaisValidas_RetornaToken()
        {
            var request = new LoginRequest { Email = "davi@teste.com", Password = "senha123" };
            var usuario = User.Criar("Davi", request.Email, "hash-correto");
            _userRepositoryMock.Setup(r => r.ObterPorEmailAsync(request.Email)).ReturnsAsync(usuario);
            _passwordHasherMock.Setup(h => h.Verify(request.Password, usuario.PasswordHash)).Returns(true);
            var expiraEm = DateTime.UtcNow.AddHours(2);
            _jwtTokenGeneratorMock
                .Setup(j => j.GerarToken(usuario.Id, usuario.Email, usuario.Nome))
                .Returns(("token-valido", expiraEm));

            var resultado = await _sut.LoginAsync(request);

            Assert.Equal("token-valido", resultado.Token);
            Assert.Equal(expiraEm, resultado.ExpiraEm);
        }
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test InvestTrack.Application.Tests/InvestTrack.Application.Tests.csproj`
Expected: build error — `AuthService` doesn't exist yet.

- [ ] **Step 4: Implement `AuthService`**

Create `InvestTrack.Application/Services/AuthService.cs`:

```csharp
using InvestTrack.Application.Dtos;
using InvestTrack.Application.Exceptions;
using InvestTrack.Application.Interfaces;
using InvestTrack.Domain.Entities;

namespace InvestTrack.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public AuthService(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var usuarioExistente = await _userRepository.ObterPorEmailAsync(request.Email);
            if (usuarioExistente is not null)
                throw new EmailJaCadastradoException(request.Email);

            var passwordHash = _passwordHasher.Hash(request.Password);
            var user = User.Criar(request.Nome, request.Email, passwordHash);

            await _userRepository.AdicionarAsync(user);

            var (token, expiraEm) = _jwtTokenGenerator.GerarToken(user.Id, user.Email, user.Nome);
            return new AuthResponse { Token = token, ExpiraEm = expiraEm };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.ObterPorEmailAsync(request.Email);
            if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
                throw new CredenciaisInvalidasException();

            var (token, expiraEm) = _jwtTokenGenerator.GerarToken(user.Id, user.Email, user.Nome);
            return new AuthResponse { Token = token, ExpiraEm = expiraEm };
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test InvestTrack.Application.Tests/InvestTrack.Application.Tests.csproj`
Expected: `Passed! - Failed: 0, Passed: 5`

- [ ] **Step 6: Commit**

```bash
git add InvestTrack.Application InvestTrack.Application.Tests InvestTrack.slnx
git commit -m "feat: add AuthService with register/login use cases"
```

---

### Task 5: Application — DI extension

**Files:**
- Create: `InvestTrack.Application/DependencyInjection.cs`
- Modify: `InvestTrack.Application/InvestTrack.Application.csproj`

`IServiceCollection` isn't available in a plain class library by default — add the abstractions package.

- [ ] **Step 1: Add the DI abstractions package**

```bash
dotnet add InvestTrack.Application/InvestTrack.Application.csproj package Microsoft.Extensions.DependencyInjection.Abstractions
```

- [ ] **Step 2: Create the DI extension**

Create `InvestTrack.Application/DependencyInjection.cs`:

```csharp
using InvestTrack.Application.Interfaces;
using InvestTrack.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InvestTrack.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            return services;
        }
    }
}
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build InvestTrack.Application/InvestTrack.Application.csproj`
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add InvestTrack.Application
git commit -m "feat: add Application DI extension"
```

---

### Task 6: Infrastructure — NuGet packages

**Files:**
- Modify: `InvestTrack.Infrastructure/InvestTrack.Infrastructure.csproj`

- [ ] **Step 1: Add all Infrastructure packages**

```bash
dotnet add InvestTrack.Infrastructure/InvestTrack.Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add InvestTrack.Infrastructure/InvestTrack.Infrastructure.csproj package BCrypt.Net-Next
dotnet add InvestTrack.Infrastructure/InvestTrack.Infrastructure.csproj package System.IdentityModel.Tokens.Jwt
dotnet add InvestTrack.Infrastructure/InvestTrack.Infrastructure.csproj package Microsoft.Extensions.Configuration.Abstractions
dotnet add InvestTrack.Infrastructure/InvestTrack.Infrastructure.csproj package Microsoft.Extensions.Options
```

- [ ] **Step 2: Build to verify restore succeeded**

Run: `dotnet build InvestTrack.Infrastructure/InvestTrack.Infrastructure.csproj`
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add InvestTrack.Infrastructure/InvestTrack.Infrastructure.csproj
git commit -m "chore: add Infrastructure NuGet packages (EF Core/Npgsql, BCrypt, JWT)"
```

---

### Task 7: Infrastructure — `AppDbContext` + `User` mapping

**Files:**
- Create: `InvestTrack.Infrastructure/Data/AppDbContext.cs`
- Create: `InvestTrack.Infrastructure/Data/Configurations/UserConfiguration.cs`

The unique index on `Email` is the real safety net against the race condition discussed in the spec — the `AuthService` check alone isn't enough under concurrent requests.

- [ ] **Step 1: Create `AppDbContext`**

Create `InvestTrack.Infrastructure/Data/AppDbContext.cs`:

```csharp
using InvestTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvestTrack.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
```

- [ ] **Step 2: Create the `User` EF Core configuration**

Create `InvestTrack.Infrastructure/Data/Configurations/UserConfiguration.cs`:

```csharp
using InvestTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestTrack.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Nome)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(320);

            builder.Property(u => u.PasswordHash)
                .IsRequired();

            builder.Property(u => u.CreatedAt)
                .IsRequired();

            builder.HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build InvestTrack.Infrastructure/InvestTrack.Infrastructure.csproj`
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add InvestTrack.Infrastructure
git commit -m "feat: add AppDbContext and User EF Core mapping with unique email index"
```

---

### Task 8: Infrastructure — `BCryptPasswordHasher`, `JwtSettings`, `JwtTokenGenerator`

**Files:**
- Create: `InvestTrack.Infrastructure/Security/BCryptPasswordHasher.cs`
- Create: `InvestTrack.Infrastructure/Security/JwtSettings.cs`
- Create: `InvestTrack.Infrastructure/Security/JwtTokenGenerator.cs`

These are thin wrappers around well-tested libraries (BCrypt.Net, System.IdentityModel.Tokens.Jwt) — per the spec's testing section, only `AuthService` gets dedicated unit tests; these are exercised indirectly by the smoke test in Task 12.

- [ ] **Step 1: Create `BCryptPasswordHasher`**

Create `InvestTrack.Infrastructure/Security/BCryptPasswordHasher.cs`:

```csharp
using InvestTrack.Application.Interfaces;

namespace InvestTrack.Infrastructure.Security
{
    public class BCryptPasswordHasher : IPasswordHasher
    {
        public string Hash(string senha) => BCrypt.Net.BCrypt.HashPassword(senha);

        public bool Verify(string senha, string hash) => BCrypt.Net.BCrypt.Verify(senha, hash);
    }
}
```

- [ ] **Step 2: Create `JwtSettings`**

Create `InvestTrack.Infrastructure/Security/JwtSettings.cs`:

```csharp
namespace InvestTrack.Infrastructure.Security
{
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string Secret { get; set; } = string.Empty;
        public int ExpirationHours { get; set; } = 2;
    }
}
```

- [ ] **Step 3: Create `JwtTokenGenerator`**

Create `InvestTrack.Infrastructure/Security/JwtTokenGenerator.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InvestTrack.Application.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InvestTrack.Infrastructure.Security
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtSettings _settings;

        public JwtTokenGenerator(IOptions<JwtSettings> options)
        {
            _settings = options.Value;
        }

        public (string Token, DateTime ExpiraEm) GerarToken(Guid userId, string email, string nome)
        {
            var expiraEm = DateTime.UtcNow.AddHours(_settings.ExpirationHours);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim("name", nome),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expiraEm,
                signingCredentials: credentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return (tokenString, expiraEm);
        }
    }
}
```

- [ ] **Step 4: Build to verify it compiles**

Run: `dotnet build InvestTrack.Infrastructure/InvestTrack.Infrastructure.csproj`
Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add InvestTrack.Infrastructure
git commit -m "feat: add BCrypt password hasher and JWT token generator"
```

---

### Task 9: Infrastructure — `UserRepository` + DI extension

**Files:**
- Create: `InvestTrack.Infrastructure/Repositories/UserRepository.cs`
- Create: `InvestTrack.Infrastructure/DependencyInjection.cs`

- [ ] **Step 1: Create `UserRepository`**

The application-level duplicate check in `AuthService` isn't enough on its own — two concurrent registrations for the same email can both pass that check before either one saves (see spec's "Concorrência: unicidade de email" section). The unique index from Task 7 is the real safety net; this repository translates the resulting Postgres unique-violation error (`SqlState 23505`) back into the same `EmailJaCadastradoException` the application-level check throws, so `AuthController` handles both cases identically with a 409.

Create `InvestTrack.Infrastructure/Repositories/UserRepository.cs`:

```csharp
using InvestTrack.Application.Exceptions;
using InvestTrack.Application.Interfaces;
using InvestTrack.Domain.Entities;
using InvestTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InvestTrack.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private const string PostgresUniqueViolationSqlState = "23505";

        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> ObterPorEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task AdicionarAsync(User user)
        {
            _context.Users.Add(user);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresUniqueViolationSqlState })
            {
                throw new EmailJaCadastradoException(user.Email);
            }
        }
    }
}
```

`PostgresException` comes from the `Npgsql` package, already pulled in transitively by `Npgsql.EntityFrameworkCore.PostgreSQL` (Task 6) — no new package needed.

- [ ] **Step 2: Create the Infrastructure DI extension**

Create `InvestTrack.Infrastructure/DependencyInjection.cs`:

```csharp
using InvestTrack.Application.Interfaces;
using InvestTrack.Infrastructure.Data;
using InvestTrack.Infrastructure.Repositories;
using InvestTrack.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvestTrack.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

            return services;
        }
    }
}
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build InvestTrack.Infrastructure/InvestTrack.Infrastructure.csproj`
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add InvestTrack.Infrastructure
git commit -m "feat: add UserRepository and Infrastructure DI extension"
```

---

### Task 10: Api — project references, packages, config

**Files:**
- Modify: `InvestTrack.Api/InvestTrack.Api.csproj`
- Modify: `InvestTrack.Api/appsettings.json`
- Modify: `InvestTrack.Api/appsettings.Development.json`

`InvestTrack.Api.csproj` currently has zero `ProjectReference`s — it needs both `Application` (for `AddApplication`/`IAuthService`) and `Infrastructure` (for `AddInfrastructure`).

- [ ] **Step 1: Add project references and packages to Api**

```bash
dotnet add InvestTrack.Api/InvestTrack.Api.csproj reference InvestTrack.Application/InvestTrack.Application.csproj
dotnet add InvestTrack.Api/InvestTrack.Api.csproj reference InvestTrack.Infrastructure/InvestTrack.Infrastructure.csproj
dotnet add InvestTrack.Api/InvestTrack.Api.csproj package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add InvestTrack.Api/InvestTrack.Api.csproj package Microsoft.EntityFrameworkCore.Design
```

- [ ] **Step 2: Update `appsettings.json` (production defaults)**

Replace the contents of `InvestTrack.Api/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": []
  },
  "Jwt": {
    "ExpirationHours": 2
  }
}
```

`Cors:AllowedOrigins` is intentionally empty here — the real Vercel domain gets set as a Render environment variable at deploy time (see Task 13), not committed to the repo. `Jwt:Secret` is deliberately absent from this file — it only ever lives in user-secrets (local) or an environment variable (Render), never in a committed file.

- [ ] **Step 3: Update `appsettings.Development.json` (local defaults)**

Replace the contents of `InvestTrack.Api/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:3000" ]
  }
}
```

- [ ] **Step 4: Build to verify it compiles**

Run: `dotnet build InvestTrack.Api/InvestTrack.Api.csproj`
Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add InvestTrack.Api
git commit -m "chore: wire Api project references, auth/EF packages and CORS/JWT config"
```

---

### Task 11: Api — JWT secret in user-secrets

**Files:** none (user-secrets store, not a repo file)

- [ ] **Step 1: Generate and store a local JWT secret**

```bash
cd InvestTrack.Api
dotnet user-secrets set "Jwt:Secret" "$(openssl rand -base64 32)"
cd ..
```

- [ ] **Step 2: Verify it was stored**

Run: `cd InvestTrack.Api && dotnet user-secrets list && cd ..`
Expected: output includes a line starting with `Jwt:Secret = ` followed by a random base64 string (alongside the existing `ConnectionStrings:DefaultConnection`).

No commit for this task — user-secrets never touch the Git working tree.

---

### Task 12: Api — `Program.cs` and `AuthController`

**Files:**
- Modify: `InvestTrack.Api/Program.cs`
- Create: `InvestTrack.Api/Controllers/AuthController.cs`

- [ ] **Step 1: Rewrite `Program.cs`**

Replace the contents of `InvestTrack.Api/Program.cs`:

```csharp
using System.Text;
using InvestTrack.Application;
using InvestTrack.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret não configurado. Rode 'dotnet user-secrets set \"Jwt:Secret\" \"...\"' localmente ou defina a env var em produção.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

- [ ] **Step 2: Create `AuthController`**

Create `InvestTrack.Api/Controllers/AuthController.cs`:

```csharp
using InvestTrack.Application.Dtos;
using InvestTrack.Application.Exceptions;
using InvestTrack.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InvestTrack.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            try
            {
                var response = await _authService.RegisterAsync(request);
                return Ok(response);
            }
            catch (EmailJaCadastradoException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);
                return Ok(response);
            }
            catch (CredenciaisInvalidasException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
    }
}
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build InvestTrack.Api/InvestTrack.Api.csproj`
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add InvestTrack.Api
git commit -m "feat: wire auth middleware in Program.cs and add AuthController"
```

---

### Task 13: Migration — generate and apply against Supabase

**Files:**
- Create: `InvestTrack.Infrastructure/Migrations/*.cs` (generated)

This creates a real `Users` table (with the unique email index) in the Supabase Postgres instance referenced by your local user-secrets.

- [ ] **Step 1: Generate the initial migration**

```bash
dotnet ef migrations add InitialCreate --project InvestTrack.Infrastructure --startup-project InvestTrack.Api --output-dir Migrations
```

Expected: `Done.` and new files under `InvestTrack.Infrastructure/Migrations/` (a `..._InitialCreate.cs`, `..._InitialCreate.Designer.cs`, and `AppDbContextModelSnapshot.cs`).

- [ ] **Step 2: Inspect the generated migration**

Open the new `..._InitialCreate.cs` file and confirm the `Up` method creates a `Users` table and a unique index on `Email` (e.g. `migrationBuilder.CreateIndex(name: "IX_Users_Email", table: "Users", column: "Email", unique: true)`).

- [ ] **Step 3: Apply the migration to Supabase**

```bash
dotnet ef database update --project InvestTrack.Infrastructure --startup-project InvestTrack.Api
```

Expected: `Done.`

- [ ] **Step 4: Verify the table exists**

Run: `dotnet ef migrations list --project InvestTrack.Infrastructure --startup-project InvestTrack.Api`
Expected: `20260723..._InitialCreate` listed with no `(pending)` marker next to it.

- [ ] **Step 5: Commit**

```bash
git add InvestTrack.Infrastructure/Migrations
git commit -m "feat: add InitialCreate migration for Users table"
```

---

### Task 14: Full test suite + manual smoke test

**Files:** none (verification only)

- [ ] **Step 1: Run the full solution build**

Run: `dotnet build InvestTrack.slnx`
Expected: `Build succeeded.` with 0 errors across all 6 projects (Domain, Application, Infrastructure, Api, Domain.Tests, Application.Tests).

- [ ] **Step 2: Run the full test suite**

Run: `dotnet test InvestTrack.slnx`
Expected: `Passed! - Failed: 0` across both test projects (7 Domain tests + 5 Application tests = 12 total).

- [ ] **Step 3: Start the API locally in the background**

```bash
dotnet run --project InvestTrack.Api
```

Run this with a background-capable tool (or a separate terminal). Wait for the log line `Now listening on: http://localhost:5158` before continuing.

- [ ] **Step 4: Smoke test — register a new user**

```bash
curl -i -X POST http://localhost:5158/api/auth/register \
  -H "Content-Type: application/json" \
  -H "Origin: http://localhost:3000" \
  -d '{"nome":"Davi Teste","email":"davi.smoke@teste.com","password":"senha123"}'
```

Expected: `HTTP/1.1 200 OK`, JSON body `{"token":"...","expiraEm":"..."}`, and header `Access-Control-Allow-Origin: http://localhost:3000` present (confirms CORS policy is active).

- [ ] **Step 5: Smoke test — duplicate email is rejected**

Run the exact same `curl` command from Step 4 again.
Expected: `HTTP/1.1 409 Conflict`, body `{"message":"O email 'davi.smoke@teste.com' já está cadastrado."}`.

- [ ] **Step 6: Smoke test — login with correct credentials**

```bash
curl -i -X POST http://localhost:5158/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"davi.smoke@teste.com","password":"senha123"}'
```

Expected: `HTTP/1.1 200 OK`, JSON body `{"token":"...","expiraEm":"..."}`.

- [ ] **Step 7: Smoke test — login with wrong password**

```bash
curl -i -X POST http://localhost:5158/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"davi.smoke@teste.com","password":"senhaErrada"}'
```

Expected: `HTTP/1.1 401 Unauthorized`, body `{"message":"Email ou senha inválidos."}`.

- [ ] **Step 8: Stop the local API**

Stop the background `dotnet run` process (Ctrl+C, or kill the background task).

No commit for this task — verification only, no file changes.

---

## After this plan

Two things remain out of scope for Claude Code and need you directly, per the spec's pendências:

1. **Render deploy config:** set `Jwt:Secret` (a fresh random value, don't reuse the local one) and `Cors:AllowedOrigins__0` (the real Vercel domain, once it exists) as environment variables in the Render dashboard. This is a manual step in an external web UI — happy to walk through the exact screen with you, or you can ask Claude in chat for a guided walkthrough with screenshots.
2. **Refresh token:** deliberately deferred, tracked in `ROADMAP.md` under Sprint 1 as a pendência for a future sprint.
