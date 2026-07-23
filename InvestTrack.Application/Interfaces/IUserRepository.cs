using InvestTrack.Domain.Entities;

namespace InvestTrack.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> ObterPorEmailAsync(string email);
        Task AdicionarAsync(User user);
    }
}
