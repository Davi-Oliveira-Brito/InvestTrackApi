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

            if (string.IsNullOrWhiteSpace(_settings.Secret) || _settings.Secret.Length < 32)
                throw new InvalidOperationException("Jwt:Secret deve ter no mínimo 32 caracteres para garantir a segurança da assinatura HS256.");
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
