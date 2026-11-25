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
        // var: idenficar automaticamente o tipo de variável
        var consultas = _consultas.Listar();
        return data is DateTime dia
            // Se não for vazia, percorre até encontrar e seleciona a Consulta específica (a partir do Where(...))
            ? consultas.Where(consulta => consulta.DataHora.Date == dia.Date).ToList()
            // Se for, retorna todas as Consultas
            : consultas;
    }

    public List<Consulta> ListarHistoricoPorPet(int petId) => _consultas.ListarPorPet(petId);

    public Consulta? ConsultarPorId(int id) => _consultas.ObterPorId(id);

    public List<string> ListarHorariosOcupados(int veterinarioId, DateTime data, int? consultaId = null)
    {
        ValidarVeterinario(veterinarioId);
        return _consultas.ListarHorariosOcupados(veterinarioId, data, consultaId);
    }

    public void Agendar(int petId, int veterinarioId, DateTime dataHora, string queixaInicial, bool urgencia)
    { 
        // Chama o construtor public do Consulta.cs
        var consulta = new Consulta(petId, veterinarioId, dataHora, queixaInicial, urgencia);
        ValidarVinculos(consulta);
        VerificarDisponibilidade(consulta);
        _consultas.Adicionar(consulta);
    }

    public void Reagendar(int id, DateTime novaDataHora, int? veterinarioId = null)
    {
        // Recupera uma consulta do banco e edita data e hora
        var consulta = ObterConsulta(id);
        consulta.Reagendar(novaDataHora, veterinarioId);
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

    public void RegistrarAtendimento(int id, string? diagnostico, string? procedimentos, string? prescricao,
        int? veterinarioId = null)
    {
        var consulta = ObterConsulta(id);
        ValidarResponsavel(consulta, veterinarioId);
        consulta.RegistrarAtendimento(diagnostico, procedimentos, prescricao);
        _consultas.Atualizar(consulta);
    }

    public void Finalizar(int id, int? veterinarioId = null)
    {
        var consulta = ObterConsulta(id);
        ValidarResponsavel(consulta, veterinarioId);
        consulta.Finalizar();
        _consultas.Atualizar(consulta);
    }

    /*  Diferentemente do método ConsultarPorId, que retorna null se o ID da Consulta não existir,
    esse método retorna uma Excessão (ou seja, ele busca Consultas que de fato existem) */
    private Consulta ObterConsulta(int id) =>
        ConsultarPorId(id) ?? throw new RegraNegocioException("A consulta não foi encontrada.");

    private static void ValidarResponsavel(Consulta consulta, int? veterinarioId)
    {
        if (veterinarioId is int responsavel && consulta.VeterinarioId != responsavel)
            throw new RegraNegocioException("O veterinário responsável mudou. Reabra a consulta para consultar o atendimento.");
    }

    private void ValidarVinculos(Consulta consulta)
    {
        if (_pets.ObterPorId(consulta.PetId) is null)
            throw new RegraNegocioException("Selecione um pet cadastrado.");
        ValidarVeterinario(consulta.VeterinarioId);
    }

    private void ValidarVeterinario(int veterinarioId)
    {
        if (_funcionarios.ObterPorId(veterinarioId) is not Veterinario { Ativo: true })
            throw new RegraNegocioException("Selecione um veterinário ativo.");
    }

    private void VerificarDisponibilidade(Consulta consulta, int? consultaIdIgnorada = null)
    {
        if (_consultas.ExisteConflitoHorario(consulta.VeterinarioId, consulta.DataHora, consultaIdIgnorada))
            throw new RegraNegocioException("O veterinário já tem uma consulta nesse horário.");
    }
}
