using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VetCare.Exceptions;
using VetCare.Models;
using VetCare.Models.Enums;
using VetCare.Models.ViewModels;
using VetCare.Models.ViewModels.Consulta;
using VetCare.Repositories.Interfaces;
using VetCare.Services;

namespace VetCare.Controllers;

public class ConsultaController : Controller
{
    private const string MensagemConsultaEncerrada = "Somente consultas agendadas podem ser alteradas.";
    private const string MensagemVeterinarioResponsavel = "Apenas o veterinário responsável pode alterar ou finalizar este atendimento.";
    private const string CookiePerfilDemonstracao = "vetcare-perfil-demonstracao";
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
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Index([FromQuery] DateTime? data = null, [FromQuery] string? areaClinica = null)
    {
        var veterinario = ObterVeterinarioSelecionado();
        var agendaVeterinaria = veterinario is not null || NormalizarAreaClinica(areaClinica) is not null;
        ViewData["AgendaVeterinaria"] = agendaVeterinaria;
        ViewData["VeterinarioSelecionadoId"] = veterinario?.Id;
        ViewData["VeterinarioSelecionadoNome"] = veterinario?.Nome;

        var agenda = ModelState.IsValid && (!agendaVeterinaria || veterinario is not null)
            ? _consultas.ListarAgenda(data)
            : [];
        if (veterinario is not null)
            agenda = agenda.Where(consulta => consulta.VeterinarioId == veterinario.Id).ToList();

        return View(nameof(Index), new ConsultaAgendaViewModel
        {
            Data = data?.Date,
            Consultas = MontarResumos(agenda)
        });
    }

    [HttpGet]
    public IActionResult Agendar() => MostrarAgendamento(new ConsultaAgendarViewModel());

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult HorariosOcupados([FromQuery] int veterinarioId, [FromQuery] DateTime? data)
    {
        if (!ModelState.IsValid || veterinarioId <= 0 || data is not DateTime dia)
            return BadRequest();

        try
        {
            return Json(_consultas.ListarHorariosOcupados(veterinarioId, dia));
        }
        catch (RegraNegocioException)
        {
            return BadRequest();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Agendar(ConsultaAgendarViewModel formulario, [FromQuery] string? areaClinica = null)
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
        return RedirectToAction(nameof(Index), NavegacaoAgenda.DaConsulta(Request.Query, areaClinica).ParaAgenda());
    }

    [HttpGet]
    public IActionResult Reagendar([FromRoute] int id, [FromQuery] string? areaClinica = null)
    {
        var consulta = _consultas.ConsultarPorId(id);
        if (consulta is null)
            return NotFound();

        if (consulta.Status != StatusConsulta.Agendada)
        {
            TempData["ConsultaErro"] = MensagemConsultaEncerrada;
            return RedirectToAction(nameof(Atendimento), NavegacaoAgenda.DaConsulta(Request.Query, areaClinica).ParaConsulta(id));
        }

        return View(nameof(Reagendar), new ConsultaReagendarViewModel
        {
            Consulta = MontarResumo(consulta),
            NovaDataHora = consulta.DataHora
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Reagendar([FromRoute] int id, ConsultaReagendarViewModel formulario,
        [FromQuery] string? areaClinica = null)
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
                var navegacao = NavegacaoAgenda.DaConsulta(Request.Query, areaClinica);
                return navegacao.VeioDoAtendimento
                    ? RedirectToAction(nameof(Atendimento), navegacao.ParaConsulta(id))
                    : RedirectToAction(nameof(Index), navegacao.ParaAgenda());
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
    public IActionResult Cancelar([FromRoute] int id, [FromQuery] string? areaClinica = null)
    {
        return Encerrar(id, consulta => _consultas.Cancelar(consulta.Id), nameof(Index), "Consulta cancelada com sucesso.",
            NavegacaoAgenda.DaConsulta(Request.Query, areaClinica).ParaAgenda());
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Atendimento([FromRoute] int id)
    {
        var consulta = _consultas.ConsultarPorId(id);
        if (consulta is null)
            return NotFound();

        return MostrarAtendimentoPersistido(consulta, ObterVeterinarioSelecionado());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Atendimento([FromRoute] int id, ConsultaAtendimentoViewModel formulario,
        [FromQuery] string? areaClinica = null)
    {
        var consulta = _consultas.ConsultarPorId(id);
        if (consulta is null)
            return NotFound();

        var veterinario = ObterVeterinarioSelecionado();
        if (veterinario is null || veterinario.Id != consulta.VeterinarioId)
        {
            ModelState.AddModelError(string.Empty, MensagemVeterinarioResponsavel);
            return MostrarAtendimentoPersistido(consulta, veterinario);
        }

        if (PodeEditar(consulta))
        {
            try
            {
                _consultas.RegistrarAtendimento(id, formulario.Diagnostico, formulario.Procedimentos,
                    formulario.Prescricao);
                TempData["ConsultaMensagem"] = "Atendimento salvo com sucesso.";
                return RedirectToAction(nameof(Atendimento),
                    NavegacaoAgenda.DaConsulta(Request.Query, "veterinario").ParaConsulta(id));
            }
            catch (RegraNegocioException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                consulta = _consultas.ConsultarPorId(id);
                if (consulta is null)
                    return NotFound();
            }
        }

        if (consulta.Status != StatusConsulta.Agendada)
        {
            // Descarta os valores do POST e mantém os erros gerais do atendimento.
            foreach (var chave in ModelState.Keys.Where(chave => chave.Length > 0).ToArray())
                ModelState.Remove(chave);

            return MostrarAtendimentoPersistido(consulta, veterinario);
        }

        ViewData["VeterinarioSelecionadoId"] = veterinario.Id;
        formulario.Consulta = MontarResumo(consulta);
        return View(nameof(Atendimento), formulario);
    }

    private IActionResult MostrarAtendimentoPersistido(Consulta consulta, Veterinario? veterinario)
    {
        ViewData["VeterinarioSelecionadoId"] = veterinario?.Id;
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
    public IActionResult Finalizar([FromRoute] int id, [FromQuery] string? areaClinica = null)
    {
        var veterinario = ObterVeterinarioSelecionado();
        return Encerrar(id, consulta =>
        {
            if (veterinario?.Id != consulta.VeterinarioId)
                throw new RegraNegocioException(MensagemVeterinarioResponsavel);

            _consultas.Finalizar(consulta.Id);
        }, nameof(Atendimento), "Atendimento finalizado com sucesso.",
            NavegacaoAgenda.DaConsulta(Request.Query, veterinario is not null ? "veterinario" : areaClinica)
                .ParaConsulta(id));
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult HistoricoPorPet([FromRoute] int id)
    {
        var pet = _pets.ObterPorId(id);
        if (pet is null)
            return NotFound();

        ViewData["VeterinarioSelecionadoId"] = ObterVeterinarioSelecionado()?.Id;
        return View(nameof(HistoricoPorPet), new ConsultaHistoricoPorPetViewModel
        {
            PetId = pet.Id,
            PetNome = pet.Nome,
            Consultas = MontarResumos(_consultas.ListarHistoricoPorPet(id))
        });
    }

    private IActionResult Encerrar(int id, Action<Consulta> operacao, string destino, string mensagem,
        object? valoresRota = null)
    {
        var consulta = _consultas.ConsultarPorId(id);
        if (consulta is null)
            return NotFound();

        try
        {
            operacao(consulta);
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

    private static string? NormalizarAreaClinica(string? areaClinica) =>
        areaClinica == "veterinario" ? areaClinica : null;

    private Veterinario? ObterVeterinarioSelecionado()
    {
        if (!int.TryParse(Request.Cookies[CookiePerfilDemonstracao], out var id) || id <= 0)
            return null;

        return _funcionarios.ObterPorId(id) is Veterinario { Ativo: true } veterinario ? veterinario : null;
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
