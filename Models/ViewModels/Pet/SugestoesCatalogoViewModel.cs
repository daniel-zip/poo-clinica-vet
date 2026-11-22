namespace VetCare.Models.ViewModels.Pet;

public sealed class SugestoesCatalogoViewModel
{
    public bool Disponivel { get; init; }
    public EspecieCadastroViewModel Especie { get; init; } = new();
    public RacaCadastroViewModel Raca { get; init; } = new();
    public IReadOnlyList<Especie> Especies { get; init; } = [];
    public IReadOnlyList<SugestaoCatalogo> Pendentes { get; init; } = [];
}
