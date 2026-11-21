using System.ComponentModel.DataAnnotations;

namespace VetCare.Models.ViewModels.Pet;

public class CatalogosViewModel
{
    public EspecieCadastroViewModel Especie { get; set; } = new();
    public RacaCadastroViewModel Raca { get; set; } = new();
    public IReadOnlyList<Especie> Especies { get; set; } = [];
    public IReadOnlyList<Raca> Racas { get; set; } = [];
}

public class EspecieCadastroViewModel
{
    [Display(Name = "Nome da espécie")]
    [Required(ErrorMessage = "Informe o nome da espécie.")]
    public string Nome { get; set; } = string.Empty;
}

public class RacaCadastroViewModel
{
    [Display(Name = "Nome da raça")]
    [Required(ErrorMessage = "Informe o nome da raça.")]
    public string Nome { get; set; } = string.Empty;

    [Display(Name = "Espécie")]
    [Required(ErrorMessage = "Selecione uma espécie.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecione uma espécie.")]
    public int? EspecieId { get; set; }
}
