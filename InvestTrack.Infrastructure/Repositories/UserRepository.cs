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
