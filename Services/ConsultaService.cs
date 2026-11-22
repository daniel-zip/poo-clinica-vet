using VetCare.Exceptions;
using VetCare.Models;
using VetCare.Repositories.Interfaces;

namespace VetCare.Services;

public class ConsultaService
{
    private readonly IConsultaRepository _consultas;
    private readonly IRepository<Pet> _pets;
    private readonly IRepository<Funcionario> _funcionarios;

    public ConsultaService(IConsultaRepository consultas, IRepository<Pet> pets,
        IRepository<Funcionario> funcionarios)
    {
        _consultas = consultas;
        _pets = pets;
        _funcionarios = funcionarios;
    }

    public List<Consulta> ListarAgenda(DateTime? data = null)
    {
        var consultas = _consultas.Listar();
        return data is DateTime dia
            ? consultas.Where(consulta => consulta.DataHora.Date == dia.Date).ToList()
            : consultas;
    }

    public List<Consulta> ListarHistoricoPorPet(int petId) => _consultas.ListarPorPet(petId);

    public Consulta? ConsultarPorId(int id) => _consultas.ObterPorId(id);

    public void Agendar(int petId, int veterinarioId, DateTime dataHora, string queixaInicial, bool urgencia)
    {
        var consulta = new Consulta(petId, veterinarioId, dataHora, queixaInicial, urgencia);
        ValidarVinculos(consulta);
        VerificarDisponibilidade(consulta);
        _consultas.Adicionar(consulta);
    }

    public void Reagendar(int id, DateTime novaDataHora)
    {
        var consulta = ObterConsulta(id);
        consulta.Reagendar(novaDataHora);
        ValidarVinculos(consulta);
        VerificarDisponibilidade(consulta, id);
        _consultas.Atualizar(consulta);
    }

    public void Cancelar(int id)
    {
        var consulta = ObterConsulta(id);
        consulta.Cancelar();
        _consultas.Atualizar(consulta);
    }

    public void RegistrarAtendimento(int id, string? diagnostico, string? procedimentos, string? prescricao)
    {
        var consulta = ObterConsulta(id);
        consulta.RegistrarAtendimento(diagnostico, procedimentos, prescricao);
        _consultas.Atualizar(consulta);
    }

    public void Finalizar(int id)
    {
        var consulta = ObterConsulta(id);
        consulta.Finalizar();
        _consultas.Atualizar(consulta);
    }

    private Consulta ObterConsulta(int id) =>
        ConsultarPorId(id) ?? throw new RegraNegocioException("A consulta não foi encontrada.");

    private void ValidarVinculos(Consulta consulta)
    {
        if (_pets.ObterPorId(consulta.PetId) is null)
            throw new RegraNegocioException("Selecione um pet cadastrado.");
        if (_funcionarios.ObterPorId(consulta.VeterinarioId) is not Veterinario)
            throw new RegraNegocioException("Selecione um veterinário cadastrado.");
    }

    private void VerificarDisponibilidade(Consulta consulta, int? consultaIdIgnorada = null)
    {
        if (_consultas.ExisteConflitoHorario(consulta.VeterinarioId, consulta.DataHora, consultaIdIgnorada))
            throw new RegraNegocioException("O veterinário já tem uma consulta agendada nesse horário.");
    }
}
