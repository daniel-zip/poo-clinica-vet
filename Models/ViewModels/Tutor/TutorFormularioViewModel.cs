using System.ComponentModel.DataAnnotations;

namespace VetCare.Models.ViewModels.Tutor;

public class TutorFormularioViewModel
{
    [Display(Name = "Nome")]
    [Required(ErrorMessage = "Informe o nome do tutor.")]
    public string Nome { get; set; } = string.Empty;

    [Display(Name = "CPF")]
    [Required(ErrorMessage = "Informe o CPF do tutor.")]
    [RegularExpression(FormatoContato.PadraoCpf, ErrorMessage = "Informe um CPF com 11 dígitos.")]
    public string Cpf { get; set; } = string.Empty;

    [Display(Name = "Telefone")]
    [Required(ErrorMessage = "Informe o telefone do tutor.")]
    [RegularExpression(FormatoContato.PadraoTelefone, ErrorMessage = "Informe o telefone com DDD e 10 ou 11 dígitos.")]
    [DataType(DataType.PhoneNumber)]
    public string Telefone { get; set; } = string.Empty;
}
