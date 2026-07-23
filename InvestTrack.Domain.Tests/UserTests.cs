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
