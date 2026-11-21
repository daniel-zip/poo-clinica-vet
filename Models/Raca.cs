using VetCare.Exceptions;

namespace VetCare.Models;

public class Raca
{
    public int Id { get; }
    public string Nome { get; }
    public int EspecieId { get; }

    public Raca(int id, string nome, int especieId)
    {
        if (id <= 0)
        {
            throw new RegraNegocioException("O identificador da raça deve ser positivo.");
        }

        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new RegraNegocioException("Informe o nome da raça.");
        }

        if (especieId <= 0)
        {
            throw new RegraNegocioException("O identificador da espécie deve ser positivo.");
        }

        Id = id;
        Nome = nome.Trim();
        EspecieId = especieId;
    }
}
