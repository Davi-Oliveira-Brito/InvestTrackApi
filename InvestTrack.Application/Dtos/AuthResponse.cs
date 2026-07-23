namespace InvestTrack.Application.Dtos
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiraEm { get; set; }
    }
}
