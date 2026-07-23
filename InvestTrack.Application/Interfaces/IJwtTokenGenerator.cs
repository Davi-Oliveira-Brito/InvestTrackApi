namespace InvestTrack.Application.Interfaces
{
    public interface IJwtTokenGenerator
    {
        (string Token, DateTime ExpiraEm) GerarToken(Guid userId, string email, string nome);
    }
}
