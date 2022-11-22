using System.ComponentModel.DataAnnotations;

namespace eAgenda.Webapi.ViewModels.ModuloAutenticacao
{
    public class AutenticarUsuarioViewModel
    {
        [Required(ErrorMessage = "o campo {0} é obrigatório")]
        [EmailAddress(ErrorMessage = "o campo {0} está em formato Inválido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "o campo {0} é obrigatório")]
        [StringLength(100, ErrorMessage = "o campo {0} precisa ter entre {2} e {1} caracteres", MinimumLength = 6)]
        public string Senha { get; set; }
    }
}
