using System.ComponentModel.DataAnnotations;
using VetCare.Models.Enums;

namespace VetCare.Models.ViewModels.Pet;

public class PetFormularioViewModel
{
    [Required(ErrorMessage = "Informe o nome do pet.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a data de nascimento.")]
    [Display(Name = "Data de nascimento")]
    [DataType(DataType.Date)]
    public DateTime? DataNascimento { get; set; }

    [Required(ErrorMessage = "Informe o peso.")]
    [Display(Name = "Peso (kg)")]
    public decimal? Peso { get; set; }

    [Required(ErrorMessage = "Selecione o sexo.")]
    public SexoPet? Sexo { get; set; }

    [Required(ErrorMessage = "Selecione um tutor.")]
    [Display(Name = "Tutor")]
    public int? TutorId { get; set; }

    [Required(ErrorMessage = "Selecione uma espécie.")]
    [Display(Name = "Espécie")]
    public int? EspecieId { get; set; }

    [Required(ErrorMessage = "Selecione uma raça.")]
    [Display(Name = "Raça")]
    public int? RacaId { get; set; }

    public IReadOnlyList<VetCare.Models.Tutor> Tutores { get; set; } = [];
    public IReadOnlyList<Especie> Especies { get; set; } = [];
    public IReadOnlyList<Raca> Racas { get; set; } = [];
}
