using InvestTrack.Application.Interfaces;

namespace InvestTrack.Infrastructure.Security
{
    public class BCryptPasswordHasher : IPasswordHasher
    {
        public string Hash(string senha) => BCrypt.Net.BCrypt.HashPassword(senha);

        public bool Verify(string senha, string hash) => BCrypt.Net.BCrypt.Verify(senha, hash);
    }
}
