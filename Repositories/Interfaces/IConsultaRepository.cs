using VetCare.Models;

namespace VetCare.Repositories.Interfaces;

public interface IConsultaRepository : IRepository<Consulta>
{
    List<Consulta> ListarPorPet(int petId);

    int ContarAgendadasAPartirDe(int veterinarioId, DateTime dataHora);

    List<string> ListarHorariosOcupados(int veterinarioId, DateTime data, int? consultaId = null);

    // Consultas canceladas liberam o horário.
    bool ExisteConflitoHorario(int veterinarioId, DateTime dataHora, int? consultaIdIgnorada = null);
}
