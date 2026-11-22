using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VetCare.Exceptions;
using VetCare.Models;
using VetCare.Models.Enums;
using VetCare.Models.ViewModels.Consulta;
using VetCare.Repositories.Interfaces;
using VetCare.Services;

namespace VetCare.Controllers;

public class ConsultaController : Controller
{
    private const string MensagemConsultaEncerrada = "Somente consultas agendadas podem ser alteradas.";
    private readonly ConsultaService _consultas;
    private readonly IRepository<Pet> _pets;
    private readonly IRepository<Funcionario> _funcionarios;

    public ConsultaController(ConsultaService consultas, IRepository<Pet> pets,
        IRepository<Funcionario> funcionarios)
    {
        _consultas = consultas;
        _pets = pets;
        _funcionarios = funcionarios;
    }

    [HttpGet]
    public IActionResult Index([FromQuery] DateTime? data = null)
    {
        return View(nameof(Index), new ConsultaAgendaViewModel
        {
            Data = data?.Date,
            Consultas = ModelState.IsValid ? MontarResumos(_consultas.ListarAgenda(data)) : []
        });
    }

    [HttpGet]
    public IActionResult Agendar() => MostrarAgendamento(new ConsultaAgendarViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Agendar(ConsultaAgendarViewModel formulario)
    {
        if (!ModelState.IsValid || formulario.PetId is not int petId ||
            formulario.VeterinarioId is not int veterinarioId || formulario.DataHora is not DateTime dataHora)
        {
            return MostrarAgendamento(formulario);
        }

        try
        {
            _consultas.Agendar(petId, veterinarioId, dataHora, formulario.QueixaInicial, formulario.Urgencia);
        }
        catch (RegraNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return MostrarAgendamento(formulario);
        }

        TempData["ConsultaMensagem"] = "Consulta agendada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Reagendar([FromRoute] int id)
    {
        var consulta = _consultas.ConsultarPorId(id);
        if (consulta is null)
            return NotFound();

        if (consulta.Status != StatusConsulta.Agendada)
        {
            TempData["ConsultaErro"] = MensagemConsultaEncerrada;
            return RedirectToAction(nameof(Atendimento), new { id });
        }

        return View(nameof(Reagendar), new ConsultaReagendarViewModel
        {
            Consulta = MontarResumo(consulta),
            NovaDataHora = consulta.DataHora
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Reagendar([FromRoute] int id, ConsultaReagendarViewModel formulario)
    {
        var consulta = _consultas.ConsultarPorId(id);
        if (consulta is null)
            return NotFound();

        if (PodeEditar(consulta) && formulario.NovaDataHora is DateTime novaDataHora)
        {
            try
            {
                _consultas.Reagendar(id, novaDataHora);
                TempData["ConsultaMensagem"] = "Consulta reagendada com sucesso.";
                return RedirectToAction(nameof(Index));
            }
            catch (RegraNegocioException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                consulta = _consultas.ConsultarPorId(id);
                if (consulta is null)
                    return NotFound();
            }
        }

        formulario.Consulta = MontarResumo(consulta);
        return View(nameof(Reagendar), formulario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cancelar([FromRoute] int id)
    {
        return Encerrar(id, _consultas.Cancelar, nameof(Index), "Consulta cancelada com sucesso.");
    }

    [HttpGet]
    public IActionResult Atendimento([FromRoute] int id)
    {
        var consulta = _consultas.ConsultarPorId(id);
        if (consulta is null)
            return NotFound();

        return View(nameof(Atendimento), new ConsultaAtendimentoViewModel
        {
            Consulta = MontarResumo(consulta),
            Diagnostico = consulta.Diagnostico,
            Procedimentos = consulta.Procedimentos,
            Prescricao = consulta.Prescricao
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Atendimento([FromRoute] int id, ConsultaAtendimentoViewModel formulario)
    {
        var consulta = _consultas.ConsultarPorId(id);
        if (consulta is null)
            return NotFound();

        if (PodeEditar(consulta))
        {
            try
            {
                _consultas.RegistrarAtendimento(id, formulario.Diagnostico, formulario.Procedimentos,
                    formulario.Prescricao);
                TempData["ConsultaMensagem"] = "Atendimento salvo com sucesso.";
                return RedirectToAction(nameof(Atendimento), new { id });
            }
            catch (RegraNegocioException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                consulta = _consultas.ConsultarPorId(id);
                if (consulta is null)
                    return NotFound();
            }
        }

        formulario.Consulta = MontarResumo(consulta);
        return View(nameof(Atendimento), formulario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Finalizar([FromRoute] int id)
    {
        return Encerrar(id, _consultas.Finalizar, nameof(Atendimento), "Atendimento finalizado com sucesso.",
            new { id });
    }

    [HttpGet]
    public IActionResult HistoricoPorPet([FromRoute] int id)
    {
        var pet = _pets.ObterPorId(id);
        if (pet is null)
            return NotFound();

        return View(nameof(HistoricoPorPet), new ConsultaHistoricoPorPetViewModel
        {
            PetId = pet.Id,
            PetNome = pet.Nome,
            Consultas = MontarResumos(_consultas.ListarHistoricoPorPet(id))
        });
    }

    private IActionResult Encerrar(int id, Action<int> operacao, string destino, string mensagem,
        object? valoresRota = null)
    {
        if (_consultas.ConsultarPorId(id) is null)
            return NotFound();

        try
        {
            operacao(id);
            TempData["ConsultaMensagem"] = mensagem;
        }
        catch (RegraNegocioException ex)
        {
            if (_consultas.ConsultarPorId(id) is null)
                return NotFound();

            TempData["ConsultaErro"] = ex.Message;
        }

        return RedirectToAction(destino, valoresRota);
    }

    private bool PodeEditar(Consulta consulta)
    {
        if (consulta.Status != StatusConsulta.Agendada)
            ModelState.AddModelError(string.Empty, MensagemConsultaEncerrada);

        return ModelState.IsValid;
    }

    private IActionResult MostrarAgendamento(ConsultaAgendarViewModel formulario)
    {
        formulario.Pets = _pets.Listar().Select(pet => Opcao(pet.Id, pet.Nome)).ToList();
        formulario.Veterinarios = _funcionarios.Listar().OfType<Veterinario>()
            .Select(veterinario => Opcao(veterinario.Id, veterinario.Nome)).ToList();
        return View(nameof(Agendar), formulario);
    }

    private static SelectListItem Opcao(int id, string nome) =>
        new(nome, id.ToString(CultureInfo.InvariantCulture));

    private ConsultaResumoViewModel MontarResumo(Consulta consulta) =>
        CriarResumo(consulta, _pets.ObterPorId(consulta.PetId)?.Nome,
            _funcionarios.ObterPorId(consulta.VeterinarioId)?.Nome);

    private List<ConsultaResumoViewModel> MontarResumos(List<Consulta> consultas)
    {
        if (consultas.Count == 0)
            return [];

        var pets = _pets.Listar().ToDictionary(pet => pet.Id, pet => pet.Nome);
        var funcionarios = _funcionarios.Listar().ToDictionary(funcionario => funcionario.Id,
            funcionario => funcionario.Nome);
        return consultas.Select(consulta => CriarResumo(consulta, pets.GetValueOrDefault(consulta.PetId),
            funcionarios.GetValueOrDefault(consulta.VeterinarioId))).ToList();
    }

    private static ConsultaResumoViewModel CriarResumo(Consulta consulta, string? petNome,
        string? veterinarioNome) => new()
    {
        Id = consulta.Id,
        PetId = consulta.PetId,
        VeterinarioId = consulta.VeterinarioId,
        PetNome = petNome ?? "—",
        VeterinarioNome = veterinarioNome ?? "—",
        DataHora = consulta.DataHora,
        Status = consulta.Status,
        QueixaInicial = consulta.QueixaInicial,
        Urgencia = consulta.Urgencia
    };
}
