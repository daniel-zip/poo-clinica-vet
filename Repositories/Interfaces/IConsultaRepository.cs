using VetCare.Models;

namespace VetCare.Repositories.Interfaces;

/* herda métodos genéricos - adicionar, listar, atualizar, excluir e obter por ID
e define operações específicas da Consulta */
public interface IConsultaRepository : IRepository<Consulta>
{
    List<Consulta> ListarPorPet(int petId);

    // Retorna a qtd de consultas de um veterinário a partir de um momento
    int ContarAgendadasAPartirDe(int veterinarioId, DateTime dataHora);

    List<string> ListarHorariosOcupados(int veterinarioId, DateTime data, int? consultaId = null);

    // Verifica se já há consulta na data/hora
    bool ExisteConflitoHorario(int veterinarioId, DateTime dataHora, int? consultaIdIgnorada = null);
}
