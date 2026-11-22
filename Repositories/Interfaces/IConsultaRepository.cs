using VetCare.Models;

namespace VetCare.Repositories.Interfaces;

public interface IConsultaRepository : IRepository<Consulta>
{
    List<Consulta> ListarPorPet(int petId);

    // Considera apenas consultas agendadas para o mesmo veterinário e horário.
    bool ExisteConflitoHorario(int veterinarioId, DateTime dataHora, int? consultaIdIgnorada = null);
}
