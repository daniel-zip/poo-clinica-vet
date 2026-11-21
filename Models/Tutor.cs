using VetCare.Exceptions;

namespace VetCare.Models;

public class Tutor
{
    private readonly List<Pet> _pets = [];

    public IReadOnlyList<Pet> Pets => _pets.AsReadOnly();
    public int Id { get; }
    public string Nome { get; private set; } = string.Empty;
    public string Cpf { get; private set; } = string.Empty;
    public string Telefone { get; private set; } = string.Empty;

    public Tutor(string nome, string cpf, string telefone) : this(0, nome, cpf, telefone)
    {
    }

    public Tutor(int id, string nome, string cpf, string telefone)
    {
        if (id < 0)
        {
            throw new RegraNegocioException("O identificador do tutor é inválido.");
        }

        Id = id;
        AtualizarDados(nome, cpf, telefone);
    }

    public void AdicionarPet(Pet pet)
    {
        if (Id <= 0 || pet.TutorId != Id)
        {
            throw new RegraNegocioException("O pet deve estar vinculado a este tutor.");
        }

        if (!_pets.Contains(pet) && (pet.Id == 0 || !_pets.Any(item => item.Id == pet.Id)))
        {
            _pets.Add(pet);
        }
    }

    public void AtualizarDados(string nome, string cpf, string telefone)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new RegraNegocioException("Informe o nome do tutor.");
        }

        if (string.IsNullOrWhiteSpace(cpf))
        {
            throw new RegraNegocioException("Informe o CPF do tutor.");
        }

        var cpfNormalizado = cpf.Trim().Replace(".", "").Replace("-", "");
        if (cpfNormalizado.Length != 11 || !cpfNormalizado.All(char.IsAsciiDigit))
        {
            throw new RegraNegocioException("Informe um CPF com 11 dígitos.");
        }

        if (string.IsNullOrWhiteSpace(telefone))
        {
            throw new RegraNegocioException("Informe o telefone do tutor.");
        }

        Nome = nome.Trim();
        Cpf = cpfNormalizado;
        Telefone = telefone.Trim();
    }
}
