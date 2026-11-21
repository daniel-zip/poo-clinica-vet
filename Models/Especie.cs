using VetCare.Exceptions;

namespace VetCare.Models;

public class Especie
{
    public int Id { get; }
    public string Nome { get; }

    public Especie(int id, string nome)
    {
        if (id <= 0)
        {
            throw new RegraNegocioException("O identificador da espécie deve ser positivo.");
        }

        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new RegraNegocioException("Informe o nome da espécie.");
        }

        Id = id;
        Nome = nome.Trim();
    }
}
