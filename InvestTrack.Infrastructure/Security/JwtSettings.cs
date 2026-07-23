namespace InvestTrack.Infrastructure.Security
{
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string Secret { get; set; } = string.Empty;
        public int ExpirationHours { get; set; } = 2;
    }
}
