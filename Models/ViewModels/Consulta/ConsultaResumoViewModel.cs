using VetCare.Models.Enums;

namespace VetCare.Models.ViewModels.Consulta;

public class ConsultaResumoViewModel
{
    public int Id { get; init; }
    public int PetId { get; init; }
    public int VeterinarioId { get; init; }
    public string PetNome { get; init; } = string.Empty;
    public string VeterinarioNome { get; init; } = string.Empty;
    public DateTime DataHora { get; init; }
    public StatusConsulta Status { get; init; }
    public string QueixaInicial { get; init; } = string.Empty;
    public bool Urgencia { get; init; }
}
