using VetCare.Exceptions;
using VetCare.Models.Enums;

namespace VetCare.Models;

public class Pet
{
    public int Id { get; }
    public string Nome { get; }
    public DateTime DataNascimento { get; }
    public decimal Peso { get; }
    public SexoPet Sexo { get; }
    public int TutorId { get; }
    public int EspecieId { get; }
    public int RacaId { get; }

    public Pet(int id, string nome, DateTime dataNascimento, decimal peso, SexoPet sexo,
        int tutorId, int especieId, Raca raca)
    {
        if (id < 0)
            throw new RegraNegocioException("O identificador do pet é inválido.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new RegraNegocioException("Informe o nome do pet.");
        if (dataNascimento == default || dataNascimento.Date > DateTime.Today)
            throw new RegraNegocioException("Informe uma data de nascimento válida.");
        if (peso <= 0)
            throw new RegraNegocioException("O peso deve ser maior que zero.");
        if (!Enum.IsDefined(sexo))
            throw new RegraNegocioException("Selecione o sexo do pet.");
        if (tutorId <= 0)
            throw new RegraNegocioException("Selecione um tutor cadastrado.");
        // A raça recebida do catálogo permite validar a relação antes de guardar apenas os IDs.
        if (raca is null || raca.EspecieId != especieId)
            throw new RegraNegocioException("Selecione uma raça da espécie escolhida.");

        Id = id;
        Nome = nome.Trim();
        DataNascimento = dataNascimento.Date;
        Peso = peso;
        Sexo = sexo;
        TutorId = tutorId;
        EspecieId = especieId;
        RacaId = raca.Id;
    }

    public int CalcularIdade()
    {
        var hoje = DateTime.Today;
        var idade = hoje.Year - DataNascimento.Year;
        // Desconta um ano se o pet ainda não fez aniversário neste ano.
        return DataNascimento.AddYears(idade) > hoje ? idade - 1 : idade;
    }
}
