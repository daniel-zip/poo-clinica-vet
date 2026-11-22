namespace VetCare.Models.ViewModels.Consulta;

public class ConsultaAgendaViewModel
{
    public DateTime? Data { get; init; }
    public IReadOnlyList<ConsultaResumoViewModel> Consultas { get; init; } = [];
}
