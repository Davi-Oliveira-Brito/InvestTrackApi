namespace InvestTrack.Domain.Entities
{
    public class User
    {
        public Guid Id { get; private set; }
        public string Nome { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private User(Guid id, string nome, string email, string passwordHash, DateTime createdAt)
        {
            Id = id;
            Nome = nome;
            Email = email;
            PasswordHash = passwordHash;
            CreatedAt = createdAt;
        }

        public static User Criar(string nome, string email, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("Nome não pode ser vazio.", nameof(nome));

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email não pode ser vazio.", nameof(email));

            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("PasswordHash não pode ser vazio.", nameof(passwordHash));

            return new User(Guid.NewGuid(), nome, email, passwordHash, DateTime.UtcNow);
        }
    }
}
