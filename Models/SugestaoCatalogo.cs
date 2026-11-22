namespace VetCare.Models;

public sealed class SugestaoCatalogo
{
    public int Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public int? EspecieId { get; init; }
    public string? EspecieNome { get; init; }
}
