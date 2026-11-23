namespace VetCare.Models.ViewModels;

public sealed record FiltrosListaViewModel(string Id, string? RotuloBusca, params FiltroLista[] Filtros);

public sealed record FiltroLista(string Chave, string Rotulo, IEnumerable<string> Opcoes);
