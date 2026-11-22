using System.ComponentModel.DataAnnotations;

namespace VetCare.Models.ViewModels.Funcionario;

public abstract class DadosFuncionarioViewModel
{
    [Required(ErrorMessage = "Informe o nome do funcionário.")]
    public string Nome { get; set; } = string.Empty;

    [Display(Name = "CPF")]
    [Required(ErrorMessage = "Informe o CPF do funcionário.")]
    [RegularExpression(FormatoContato.PadraoCpf, ErrorMessage = "Informe um CPF com 11 dígitos.")]
    public string Cpf { get; set; } = string.Empty;

    [Display(Name = "E-mail")]
    [Required(ErrorMessage = "Informe o e-mail do funcionário.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o telefone do funcionário.")]
    [RegularExpression(FormatoContato.PadraoTelefone, ErrorMessage = "Informe o telefone com DDD e 10 ou 11 dígitos.")]
    public string Telefone { get; set; } = string.Empty;

    [Display(Name = "Endereço")]
    [Required(ErrorMessage = "Informe o endereço do funcionário.")]
    public string Endereco { get; set; } = string.Empty;

    [Display(Name = "Salário")]
    [Required(ErrorMessage = "Informe o salário do funcionário.")]
    [Range(0.0, double.MaxValue, ErrorMessage = "Informe um salário válido e não negativo.")]
    public double? Salario { get; set; }

    public string? Turno { get; set; }

    [Display(Name = "CRMV")]
    public string? Crmv { get; set; }

    public string? Especialidade { get; set; }
}

public sealed class CadastrarFuncionarioViewModel : DadosFuncionarioViewModel
{
    [Required(ErrorMessage = "Informe o tipo do funcionário.")]
    public string Tipo { get; set; } = string.Empty;
}

public sealed class EditarFuncionarioViewModel : DadosFuncionarioViewModel
{
}
