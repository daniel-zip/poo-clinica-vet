using System.ComponentModel.DataAnnotations;

namespace VetCare.Models.ViewModels.Funcionario;

public abstract class DadosFuncionarioViewModel
{
    [Required(ErrorMessage = "Informe o nome do funcionário.")]
    public string Nome { get; set; } = string.Empty;

    [Display(Name = "CPF")]
    [Required(ErrorMessage = "Informe o CPF do funcionário.")]
    public string Cpf { get; set; } = string.Empty;

    [Display(Name = "E-mail")]
    [Required(ErrorMessage = "Informe o e-mail do funcionário.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o telefone do funcionário.")]
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
