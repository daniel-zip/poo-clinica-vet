namespace VetCare.Models.ViewModels.Consulta;

public class ConsultaHistoricoPorPetViewModel
{
    public int PetId { get; init; }
    public string PetNome { get; init; } = string.Empty;
    public IReadOnlyList<ConsultaResumoViewModel> Consultas { get; init; } = [];
}
