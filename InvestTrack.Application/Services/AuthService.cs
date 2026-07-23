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
