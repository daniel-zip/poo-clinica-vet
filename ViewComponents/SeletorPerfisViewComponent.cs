using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using VetCare.Models;
using VetCare.Repositories.Interfaces;

namespace VetCare.ViewComponents;

public sealed class SeletorPerfisViewComponent(IRepository<Funcionario> funcionarios) : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var ordemAlfabetica = StringComparer.Create(CultureInfo.GetCultureInfo("pt-BR"), ignoreCase: true);
        var perfis = funcionarios.Listar()
            .Where(funcionario => funcionario.Ativo)
            .Select(CriarPerfil)
            .OrderBy(perfil => perfil.Cargo, ordemAlfabetica)
            .ThenBy(perfil => perfil.Nome, ordemAlfabetica)
            .ToList();

        return View(perfis);
    }

    private static PerfilDemonstracao CriarPerfil(Funcionario funcionario)
    {
        var (cargo, area) = funcionario switch
        {
            Recepcionista => ("Recepcionista", "recepcao"),
            Veterinario => ("Veterinário", "veterinario"),
            Administrador => ("Administrador", "administracao"),
            _ => throw new InvalidOperationException("Cargo de funcionário não reconhecido.")
        };

        var nomes = funcionario.Nome.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var iniciais = nomes.Length == 1
            ? nomes[0][..1]
            : string.Concat(nomes[0][0], nomes[^1][0]);

        return new PerfilDemonstracao(funcionario.Id, funcionario.Nome, iniciais.ToUpperInvariant(), cargo, area);
    }
}

public sealed record PerfilDemonstracao(int Id, string Nome, string Iniciais, string Cargo, string Area);
