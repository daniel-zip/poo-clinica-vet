using System.ComponentModel.DataAnnotations;

namespace VetCare.Models.ViewModels.Tutor;

public class TutorFormularioViewModel
{
    [Display(Name = "Nome")]
    [Required(ErrorMessage = "Informe o nome do tutor.")]
    public string Nome { get; set; } = string.Empty;

    [Display(Name = "CPF")]
    [Required(ErrorMessage = "Informe o CPF do tutor.")]
    public string Cpf { get; set; } = string.Empty;

    [Display(Name = "Telefone")]
    [Required(ErrorMessage = "Informe o telefone do tutor.")]
    [DataType(DataType.PhoneNumber)]
    public string Telefone { get; set; } = string.Empty;
}
