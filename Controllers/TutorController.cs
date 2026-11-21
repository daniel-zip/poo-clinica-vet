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

        foreach (var pet in _pets.ListarPorTutor(id))
        {
            tutor.AdicionarPet(pet);
        }

        return View(tutor);
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
    public IActionResult Editar(int id)
    {
        var tutor = _tutores.ObterPorId(id);
        if (tutor is null)
        {
            return TutorNaoEncontrado();
        }

        ViewData["TutorId"] = tutor.Id;
        return View(new TutorFormularioViewModel
        {
            Nome = tutor.Nome,
            Cpf = tutor.Cpf,
            Telefone = tutor.Telefone
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Editar(int id, TutorFormularioViewModel formulario)
    {
        var tutor = _tutores.ObterPorId(id);
        if (tutor is null)
        {
            return TutorNaoEncontrado();
        }

        ViewData["TutorId"] = tutor.Id;
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
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Excluir(int id)
    {
        var tutor = _tutores.ObterPorId(id);
        if (tutor is null)
        {
            return TutorNaoEncontrado();
        }

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
            return View(nameof(Excluir), tutor);
        }

        TempData["TutorMensagem"] = "Tutor excluído com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private IActionResult TutorNaoEncontrado()
    {
        TempData["TutorErro"] = "O tutor não foi encontrado.";
        return RedirectToAction(nameof(Index));
    }
}
