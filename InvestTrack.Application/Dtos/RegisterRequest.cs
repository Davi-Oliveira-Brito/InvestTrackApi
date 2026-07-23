using System.ComponentModel.DataAnnotations;

namespace InvestTrack.Application.Dtos
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Nome é obrigatório.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Email em formato inválido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Senha é obrigatória.")]
        [MinLength(8, ErrorMessage = "Senha deve ter no mínimo 8 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Senha deve conter ao menos uma letra e um número.")]
        public string Password { get; set; } = string.Empty;
    }
}
