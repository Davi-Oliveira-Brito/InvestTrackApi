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
