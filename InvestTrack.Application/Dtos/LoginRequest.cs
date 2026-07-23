using System.ComponentModel.DataAnnotations;

namespace InvestTrack.Application.Dtos
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Email em formato inválido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Senha é obrigatória.")]
        public string Password { get; set; } = string.Empty;
    }
}
