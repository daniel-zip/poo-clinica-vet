using Microsoft.AspNetCore.Mvc;
using VetCare.Exceptions;
using VetCare.Models;
using VetCare.Models.ViewModels.Pet;
using VetCare.Repositories.Dapper;
using VetCare.Repositories.Interfaces;

namespace VetCare.Controllers;

public class PetController : Controller
{
    private readonly PetRepository _pets;
    private readonly IRepository<Tutor> _tutores;

    public PetController(PetRepository pets, IRepository<Tutor> tutores)
    {
        _pets = pets;
        _tutores = tutores;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(MontarResumo(_pets.Listar()));
    }

    [HttpGet]
    public IActionResult Detalhes(int id)
    {
        var pet = _pets.ObterPorId(id);
        return pet is null ? PetNaoEncontrado() : View(MontarResumo([pet]).Single());
    }

    [HttpGet]
    public IActionResult Cadastrar()
    {
        return MostrarFormulario(nameof(Cadastrar), new PetFormularioViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cadastrar(PetFormularioViewModel formulario)
    {
        return Salvar(nameof(Cadastrar), 0, formulario);
    }

    [HttpGet]
    public IActionResult Editar(int id, int? tutorOrigemId, [FromQuery] string? origem = null)
    {
        var pet = _pets.ObterPorId(id);
        if (pet is null)
            return PetNaoEncontrado();

        ViewData["TutorOrigemId"] = tutorOrigemId;
        ViewData["VoltarFicha"] = origem == "ficha";
        return MostrarFormulario(nameof(Editar), new PetFormularioViewModel
        {
            Nome = pet.Nome, DataNascimento = pet.DataNascimento, Peso = pet.Peso,
            Sexo = pet.Sexo, TutorId = pet.TutorId, EspecieId = pet.EspecieId, RacaId = pet.RacaId
        }, id);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Editar(int id, PetFormularioViewModel formulario, int? tutorOrigemId,
        [FromQuery] string? origem = null)
    {
        if (_pets.ObterPorId(id) is null)
            return PetNaoEncontrado();

        ViewData["TutorOrigemId"] = tutorOrigemId;
        ViewData["VoltarFicha"] = origem == "ficha";
        return Salvar(nameof(Editar), id, formulario, tutorOrigemId);
    }

    [HttpGet]
    public IActionResult Excluir(int id, int? tutorOrigemId)
    {
        ViewData["TutorOrigemId"] = tutorOrigemId;
        var pet = _pets.ObterPorId(id);
        return pet is null ? PetNaoEncontrado() : View(pet);
    }

    [HttpPost]
    [ActionName(nameof(Excluir))]
    [ValidateAntiForgeryToken]
    public IActionResult ConfirmarExclusao(int id, int? tutorOrigemId)
    {
        var pet = _pets.ObterPorId(id);
        if (pet is null)
            return PetNaoEncontrado();

        try
        {
            _pets.Excluir(id);
        }
        catch (RegraNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewData["TutorOrigemId"] = tutorOrigemId;
            return View(nameof(Excluir), pet);
        }

        TempData["PetMensagem"] = "Pet excluído com sucesso.";
        return VoltarAposSalvar(tutorOrigemId);
    }

    private IActionResult Salvar(string view, int id, PetFormularioViewModel formulario, int? tutorOrigemId = null)
    {
        if (!ModelState.IsValid || formulario.DataNascimento is not DateTime nascimento ||
            formulario.Peso is not decimal peso || formulario.Sexo is not { } sexo ||
            formulario.TutorId is not int tutorId || formulario.EspecieId is not int especieId ||
            formulario.RacaId is not int racaId)
        {
            return MostrarFormulario(view, formulario, id);
        }

        // Os IDs do formulário são conferidos no servidor, inclusive a relação entre espécie e raça.
        var tutor = _tutores.ObterPorId(tutorId);
        var raca = _pets.ObterRacaPorId(racaId);
        if (tutor is null)
            ModelState.AddModelError(nameof(formulario.TutorId), "Selecione um tutor cadastrado.");
        if (raca is null || raca.EspecieId != especieId)
            ModelState.AddModelError(nameof(formulario.RacaId), "Selecione uma raça da espécie escolhida.");
        if (tutor is null || raca is null || !ModelState.IsValid)
            return MostrarFormulario(view, formulario, id);

        try
        {
            var pet = new Pet(id, formulario.Nome, nascimento, peso, sexo, tutor.Id, especieId, raca);
            tutor.AdicionarPet(pet);
            if (id == 0)
                _pets.Adicionar(pet);
            else
                _pets.Atualizar(pet);
        }
        catch (RegraNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return MostrarFormulario(view, formulario, id);
        }

        TempData["PetMensagem"] = id == 0 ? "Pet cadastrado com sucesso." : "Pet atualizado com sucesso.";
        return VoltarAposSalvar(tutorOrigemId, id);
    }

    private RedirectToActionResult VoltarAposSalvar(int? tutorOrigemId, int petId = 0)
    {
        // Quando a edição começou na exclusão do tutor, volta à conferência dos vínculos restantes.
        if (tutorOrigemId is int id)
            return RedirectToAction(nameof(TutorController.Excluir), "Tutor", new { id });

        return petId > 0 && ViewData["VoltarFicha"] is true
            ? RedirectToAction(nameof(Detalhes), new { id = petId })
            : RedirectToAction(nameof(Index));
    }

    private IActionResult MostrarFormulario(string view, PetFormularioViewModel formulario, int id = 0)
    {
        ViewData["PetId"] = id;
        formulario.Tutores = _tutores.Listar();
        formulario.Especies = _pets.ListarEspecies();
        formulario.Racas = _pets.ListarRacas();
        return View(view, formulario);
    }

    private List<PetResumoViewModel> MontarResumo(IEnumerable<Pet> pets)
    {
        var tutores = _tutores.Listar().ToDictionary(tutor => tutor.Id, tutor => tutor.Nome);
        var especies = _pets.ListarEspecies().ToDictionary(especie => especie.Id, especie => especie.Nome);
        var racas = _pets.ListarRacas().ToDictionary(raca => raca.Id, raca => raca.Nome);

        return pets.Select(pet => new PetResumoViewModel(pet, tutores.GetValueOrDefault(pet.TutorId, "—"),
            especies.GetValueOrDefault(pet.EspecieId, "—"), racas.GetValueOrDefault(pet.RacaId, "—"))).ToList();
    }

    private IActionResult PetNaoEncontrado()
    {
        TempData["PetErro"] = "O pet não foi encontrado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Administracao/Catalogos")]
    public IActionResult Catalogos()
    {
        return MostrarCatalogos(new CatalogosViewModel());
    }

    [HttpGet("Recepcao/SugestoesCatalogo")]
    public IActionResult SugerirCatalogo() => MostrarFormularioDeSugestao();

    [HttpPost("Recepcao/SugestoesCatalogo/Especie")]
    [ValidateAntiForgeryToken]
    public IActionResult SugerirEspecie([Bind(Prefix = "Especie")] EspecieCadastroViewModel formulario)
    {
        if (ModelState.IsValid && EnviarSugestao(formulario.Nome))
            return RedirectToAction(nameof(SugerirCatalogo));

        return MostrarFormularioDeSugestao(especie: formulario);
    }

    [HttpPost("Recepcao/SugestoesCatalogo/Raca")]
    [ValidateAntiForgeryToken]
    public IActionResult SugerirRaca([Bind(Prefix = "Raca")] RacaCadastroViewModel formulario)
    {
        if (ModelState.IsValid && formulario.EspecieId is int especieId && EnviarSugestao(formulario.Nome, especieId))
            return RedirectToAction(nameof(SugerirCatalogo));

        return MostrarFormularioDeSugestao(raca: formulario);
    }

    [HttpGet("Administracao/Catalogos/Sugestoes")]
    public IActionResult SugestoesCatalogo()
    {
        var pendentes = _pets.ListarSugestoes();
        return View(new SugestoesCatalogoViewModel { Disponivel = pendentes is not null, Pendentes = pendentes ?? [] });
    }

    [HttpPost("Administracao/Catalogos/Sugestoes/{id:int}/Aprovar")]
    [ValidateAntiForgeryToken]
    public IActionResult AprovarSugestao([FromRoute] int id)
    {
        try
        {
            TempData["SugestaoMensagem"] = _pets.AprovarSugestao(id)
                ? "Sugestão aprovada. O item já está disponível no catálogo."
                : "O item já estava no catálogo. A sugestão foi concluída.";
        }
        catch (RegraNegocioException ex)
        {
            TempData["SugestaoErro"] = ex.Message;
        }

        return RedirectToAction(nameof(SugestoesCatalogo));
    }

    [HttpPost("Administracao/Catalogos/Sugestoes/{id:int}/Reprovar")]
    [ValidateAntiForgeryToken]
    public IActionResult ReprovarSugestao([FromRoute] int id)
    {
        try
        {
            _pets.ReprovarSugestao(id);
            TempData["SugestaoMensagem"] = "Sugestão reprovada e removida da fila.";
        }
        catch (RegraNegocioException ex)
        {
            TempData["SugestaoErro"] = ex.Message;
        }

        return RedirectToAction(nameof(SugestoesCatalogo));
    }

    [HttpPost("Administracao/Catalogos/Especies")]
    [ValidateAntiForgeryToken]
    public IActionResult CadastrarEspecie([Bind(Prefix = "Especie")] EspecieCadastroViewModel formulario)
    {
        if (!ModelState.IsValid)
        {
            return MostrarCatalogos(new CatalogosViewModel { Especie = formulario });
        }

        try
        {
            _pets.AdicionarEspecie(formulario.Nome);
        }
        catch (RegraNegocioException ex)
        {
            ModelState.AddModelError("Especie.Nome", ex.Message);
            return MostrarCatalogos(new CatalogosViewModel { Especie = formulario });
        }

        TempData["CatalogosMensagem"] = "Espécie cadastrada com sucesso.";
        return RedirectToAction(nameof(Catalogos));
    }

    [HttpPost("Administracao/Catalogos/Racas")]
    [ValidateAntiForgeryToken]
    public IActionResult CadastrarRaca([Bind(Prefix = "Raca")] RacaCadastroViewModel formulario)
    {
        if (!ModelState.IsValid || formulario.EspecieId is not int especieId)
        {
            return MostrarCatalogos(new CatalogosViewModel { Raca = formulario });
        }

        try
        {
            _pets.AdicionarRaca(formulario.Nome, especieId);
        }
        catch (RegraNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return MostrarCatalogos(new CatalogosViewModel { Raca = formulario });
        }

        TempData["CatalogosMensagem"] = "Raça cadastrada com sucesso.";
        return RedirectToAction(nameof(Catalogos));
    }

    private IActionResult MostrarCatalogos(CatalogosViewModel formulario)
    {
        formulario.Especies = _pets.ListarEspecies();
        formulario.Racas = _pets.ListarRacas();
        return View(nameof(Catalogos), formulario);
    }

    private IActionResult MostrarFormularioDeSugestao(EspecieCadastroViewModel? especie = null,
        RacaCadastroViewModel? raca = null)
    {
        return View(nameof(SugerirCatalogo), new SugestoesCatalogoViewModel
        {
            Disponivel = _pets.SugestoesDisponiveis(),
            Especie = especie ?? new(),
            Raca = raca ?? new(),
            Especies = _pets.ListarEspecies()
        });
    }

    private bool EnviarSugestao(string nome, int? especieId = null)
    {
        try
        {
            _pets.SugerirItemCatalogo(nome, especieId);
            TempData["SugestaoMensagem"] = "Sugestão enviada para revisão da Administração.";
            return true;
        }
        catch (RegraNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return false;
        }
    }
}
