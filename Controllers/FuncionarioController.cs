using Microsoft.AspNetCore.Mvc;
using VetCare.Exceptions;
using VetCare.Models;
using VetCare.Models.Enums;
using VetCare.Models.ViewModels.Funcionario;
using VetCare.Repositories.Interfaces;
using VetCare.Services;

namespace VetCare.Controllers;

public class FuncionarioController : Controller
{
    private readonly IRepository<Funcionario> _funcionarios;

    public FuncionarioController(IRepository<Funcionario> funcionarios)
    {
        _funcionarios = funcionarios;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(_funcionarios.Listar());
    }

    [HttpGet]
    public IActionResult Detalhes([FromRoute] int id)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var funcionario = _funcionarios.ObterPorId(id);
        if (funcionario is null)
        {
            return NotFound();
        }

        ViewData["Operacoes"] = Enum.GetValues<Operacao>()
            .ToDictionary(operacao => operacao, funcionario.PodeExecutar);

        return View(funcionario);
    }


    [HttpGet]
    public IActionResult Cadastrar()
    {
        return View(new CadastrarFuncionarioViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cadastrar([FromForm] CadastrarFuncionarioViewModel formulario)
    {
        var tipo = formulario.Tipo?.Trim() ?? string.Empty;
        var nivelAcesso = tipo switch
        {
            nameof(Recepcionista) => 1,
            nameof(Veterinario) => 2,
            nameof(Administrador) => 3,
            _ => 0
        };

        if (nivelAcesso == 0 && tipo.Length > 0)
        {
            ModelState.AddModelError(nameof(formulario.Tipo), "Selecione um tipo de funcionário válido.");
        }

        ValidarCamposDoCargo(tipo, formulario);
        if (!ModelState.IsValid || nivelAcesso == 0 || formulario.Salario is not double salario)
        {
            return View(formulario);
        }

        try
        {
            var funcionario = FuncionarioFactory.Criar(
                tipo, id: 0, formulario.Nome, formulario.Cpf, formulario.Email,
                formulario.Telefone, formulario.Endereco, salario, nivelAcesso, ativo: true,
                formulario.Turno, formulario.Crmv, formulario.Especialidade);
            _funcionarios.Adicionar(funcionario);
        }
        catch (RegraNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(formulario);
        }

        TempData["FuncionarioMensagem"] = "Funcionário cadastrado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Editar([FromRoute] int id)
    {
        var funcionario = ObterFuncionario(id);
        if (funcionario is null)
        {
            return NotFound();
        }

        var formulario = new EditarFuncionarioViewModel
        {
            Nome = funcionario.Nome,
            Cpf = funcionario.Cpf,
            Email = funcionario.Email,
            Telefone = funcionario.Telefone,
            Endereco = funcionario.Endereco,
            Salario = funcionario.Salario,
            Turno = (funcionario as Recepcionista)?.Turno,
            Crmv = (funcionario as Veterinario)?.Crmv,
            Especialidade = (funcionario as Veterinario)?.Especialidade
        };
        return MostrarEdicao(funcionario, formulario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Editar([FromRoute] int id, [FromForm] EditarFuncionarioViewModel formulario)
    {
        var funcionario = ObterFuncionario(id);
        if (funcionario is null)
        {
            return NotFound();
        }

        ValidarCamposDoCargo(funcionario.GetType().Name, formulario);
        if (!ModelState.IsValid || formulario.Salario is not double salario)
        {
            return MostrarEdicao(funcionario, formulario);
        }

        try
        {
            AtualizarInformacoes(funcionario, formulario, salario);
            _funcionarios.Atualizar(funcionario);
        }
        catch (RegraNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return MostrarEdicao(funcionario, formulario);
        }

        TempData["FuncionarioMensagem"] = "Funcionário atualizado com sucesso.";
        return RedirectToAction(nameof(Detalhes), new { id });
    }

    [HttpGet]
    public IActionResult Desativar([FromRoute] int id)
    {
        var funcionario = ObterFuncionario(id);
        return funcionario is null ? NotFound() : View(funcionario);
    }

    [HttpPost]
    [ActionName(nameof(Desativar))]
    [ValidateAntiForgeryToken]
    public IActionResult ConfirmarDesativacao([FromRoute] int id)
    {
        var funcionario = ObterFuncionario(id);
        if (funcionario is null)
        {
            return NotFound();
        }

        if (!funcionario.Ativo)
        {
            ModelState.AddModelError(string.Empty, "O funcionário já está desativado.");
            return View(nameof(Desativar), funcionario);
        }

        try
        {
            _funcionarios.Excluir(id);
        }
        catch (RegraNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(nameof(Desativar), funcionario);
        }

        TempData["FuncionarioMensagem"] = "Funcionário desativado com sucesso.";
        return RedirectToAction(nameof(Detalhes), new { id });
    }

    private Funcionario? ObterFuncionario(int id) => id > 0 ? _funcionarios.ObterPorId(id) : null;

    private IActionResult MostrarEdicao(Funcionario funcionario, EditarFuncionarioViewModel formulario)
    {
        ViewData["FuncionarioId"] = funcionario.Id;
        ViewData["Tipo"] = funcionario.GetType().Name;
        return View(nameof(Editar), formulario);
    }

    private void ValidarCamposDoCargo(string tipo, DadosFuncionarioViewModel formulario)
    {
        if (tipo == nameof(Recepcionista) && string.IsNullOrWhiteSpace(formulario.Turno))
        {
            ModelState.AddModelError(nameof(formulario.Turno), "Informe o turno do recepcionista.");
        }

        if (tipo == nameof(Veterinario))
        {
            if (string.IsNullOrWhiteSpace(formulario.Crmv))
            {
                ModelState.AddModelError(nameof(formulario.Crmv), "Informe o CRMV do veterinário.");
            }

            if (string.IsNullOrWhiteSpace(formulario.Especialidade))
            {
                ModelState.AddModelError(nameof(formulario.Especialidade), "Informe a especialidade do veterinário.");
            }
        }
    }

    private static void AtualizarInformacoes(Funcionario funcionario, EditarFuncionarioViewModel formulario, double salario)
    {
        var nivelAcesso = funcionario.NivelAcesso;
        switch (funcionario)
        {
            case Recepcionista recepcionista:
                recepcionista.AtualizarInformacoes(
                    formulario.Nome, formulario.Cpf, formulario.Email, formulario.Telefone,
                    formulario.Endereco, salario, nivelAcesso, formulario.Turno ?? string.Empty);
                break;
            case Veterinario veterinario:
                veterinario.AtualizarInformacoes(
                    formulario.Nome, formulario.Cpf, formulario.Email, formulario.Telefone,
                    formulario.Endereco, salario, nivelAcesso,
                    formulario.Crmv ?? string.Empty, formulario.Especialidade ?? string.Empty);
                break;
            default:
                funcionario.AtualizarInformacoes(
                    formulario.Nome, formulario.Cpf, formulario.Email, formulario.Telefone,
                    formulario.Endereco, salario, nivelAcesso);
                break;
        }
    }
}
