namespace InvestTrack.Application.Exceptions
{
    public class EmailJaCadastradoException : Exception
    {
        public EmailJaCadastradoException(string email)
            : base($"O email '{email}' já está cadastrado.")
        {
        }
    }
}
