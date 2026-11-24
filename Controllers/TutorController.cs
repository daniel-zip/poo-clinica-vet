using Microsoft.AspNetCore.Mvc;
using VetCare.Exceptions;
using VetCare.Models;
using VetCare.Models.ViewModels.Tutor;
using VetCare.Repositories.Dapper;
using VetCare.Repositories.Interfaces;

namespace VetCare.Controllers;

public class TutorController : Controller
{
    private readonly IRepository<Tutor> _tutores;
    private readonly PetRepository _pets;

    public TutorController(IRepository<Tutor> tutores, PetRepository pets)
    {
        _tutores = tutores;
        _pets = pets;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(_tutores.Listar());
    }

    [HttpGet]
    public IActionResult Detalhes(int id)
    {
        var tutor = _tutores.ObterPorId(id);
        if (tutor is null)
        {
            return TutorNaoEncontrado();
        }

        CarregarPets(tutor);
        return View(tutor);
    }

    [HttpGet]
    public IActionResult VincularPets(int id, [FromQuery] int[]? petIds = null)
    {
        var tutor = _tutores.ObterPorId(id);
        return tutor is null ? TutorNaoEncontrado() : View(MontarSelecaoDePets(tutor, petIds ?? []));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RevisarVinculos(int id, [FromForm] int[] petIds)
    {
        var tutor = _tutores.ObterPorId(id);
        if (tutor is null)
            return TutorNaoEncontrado();

        // Recarrega os pets antes da revisão para mostrar os tutores atuais dos registros selecionados.
        var selecao = MontarSelecaoDePets(tutor, petIds);
        var pets = selecao.Pets.Where(pet => selecao.Selecionados.Contains(pet.Id)).ToList();
        if (!ModelState.IsValid || pets.Count != selecao.Selecionados.Count)
        {
            ModelState.AddModelError(string.Empty, "Revise a seleção de pets.");
            return View(nameof(VincularPets), selecao);
        }
        if (pets.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Selecione pelo menos um pet.");
            return View(nameof(VincularPets), selecao);
        }

        return View(selecao with { Pets = pets });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VincularPets(int id, [FromForm] Dictionary<int, int> vinculos)
    {
        var tutor = _tutores.ObterPorId(id);
        if (tutor is null)
            return TutorNaoEncontrado();

        try
        {
            if (!ModelState.IsValid)
                throw new RegraNegocioException("Revise a seleção de pets.");

            var total = _pets.TransferirParaTutor(id, vinculos);
            TempData["TutorPetsMensagem"] = total == 1
                ? "Pet vinculado com sucesso."
                : $"{total} pets vinculados com sucesso.";
        }
        catch (RegraNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(nameof(VincularPets), MontarSelecaoDePets(tutor, vinculos.Keys));
        }

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    [HttpGet]
    public IActionResult Cadastrar()
    {
        return View(new TutorFormularioViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cadastrar(TutorFormularioViewModel formulario)
    {
        if (!ModelState.IsValid)
        {
            return View(formulario);
        }

        try
        {
            _tutores.Adicionar(new Tutor(formulario.Nome, formulario.Cpf, formulario.Telefone));
        }
        catch (RegraNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(formulario);
        }

        TempData["TutorMensagem"] = "Tutor cadastrado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Editar(int id, [FromQuery] string? origem = null)
    {
        var tutor = _tutores.ObterPorId(id);
        if (tutor is null)
        {
            return TutorNaoEncontrado();
        }

        ViewData["TutorId"] = tutor.Id;
        ViewData["VoltarFicha"] = origem == "ficha";
        return View(new TutorFormularioViewModel
        {
            Nome = tutor.Nome,
            Cpf = tutor.Cpf,
            Telefone = tutor.Telefone
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Editar(int id, TutorFormularioViewModel formulario, [FromQuery] string? origem = null)
    {
        var tutor = _tutores.ObterPorId(id);
        if (tutor is null)
        {
            return TutorNaoEncontrado();
        }

        ViewData["TutorId"] = tutor.Id;
        ViewData["VoltarFicha"] = origem == "ficha";
        if (!ModelState.IsValid)
        {
            return View(formulario);
        }

        try
        {
            tutor.AtualizarDados(formulario.Nome, formulario.Cpf, formulario.Telefone);
            _tutores.Atualizar(tutor);
        }
        catch (RegraNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(formulario);
        }

        TempData["TutorMensagem"] = "Tutor atualizado com sucesso.";
        return origem == "ficha"
            ? RedirectToAction(nameof(Detalhes), new { id })
            : RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Excluir(int id)
    {
        var tutor = _tutores.ObterPorId(id);
        if (tutor is null)
        {
            return TutorNaoEncontrado();
        }

        CarregarPets(tutor);
        return View(tutor);
    }

    [HttpPost]
    [ActionName(nameof(Excluir))]
    [ValidateAntiForgeryToken]
    public IActionResult ConfirmarExclusao(int id)
    {
        var tutor = _tutores.ObterPorId(id);
        if (tutor is null)
        {
            return TutorNaoEncontrado();
        }

        try
        {
            _tutores.Excluir(id);
        }
        catch (RegraNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            CarregarPets(tutor);
            return View(nameof(Excluir), tutor);
        }

        TempData["TutorMensagem"] = "Tutor excluído com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private VincularPetsViewModel MontarSelecaoDePets(Tutor tutor, IEnumerable<int> selecionados)
    {
        var nomesTutores = _tutores.Listar().ToDictionary(item => item.Id, item => item.Nome);
        var nomesRacas = _pets.ListarRacas().ToDictionary(item => item.Id, item => item.Nome);
        var disponiveis = _pets.Listar()
            .Where(pet => pet.TutorId != tutor.Id)
            .Select(pet => new PetDisponivelViewModel(pet.Id, pet.Nome, pet.TutorId,
                nomesRacas.GetValueOrDefault(pet.RacaId, "—"),
                nomesTutores.GetValueOrDefault(pet.TutorId, "—")))
            .ToList();

        return new VincularPetsViewModel(tutor.Id, tutor.Nome, disponiveis, selecionados.ToHashSet());
    }

    private void CarregarPets(Tutor tutor)
    {
        foreach (var pet in _pets.ListarPorTutor(tutor.Id))
        {
            tutor.AdicionarPet(pet);
        }
    }

    private IActionResult TutorNaoEncontrado()
    {
        TempData["TutorErro"] = "O tutor não foi encontrado.";
        return RedirectToAction(nameof(Index));
    }
}
