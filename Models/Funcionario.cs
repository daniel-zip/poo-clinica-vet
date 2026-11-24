using VetCare.Exceptions;
using VetCare.Models.Enums;

namespace VetCare.Models;

public abstract class Funcionario //criação de classe abstrata Funcionario com elementos comuns à todos os funcionários
{
    public int Id { get; }
    public string Nome { get; private set; } = string.Empty;
    public string Cpf { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Telefone { get; private set; } = string.Empty;
    public string Endereco { get; private set; } = string.Empty;
    public double Salario { get; private set; }
    public int NivelAcesso { get; private set; }
    public bool Ativo { get; private set; } //propriedades do funcionário

    protected Funcionario(  //construtor da classe
        int id,
        string nome,
        string cpf,
        string email,
        string telefone,
        string endereco,
        double salario,
        int nivelAcesso,
        bool ativo = true)
    {
        if (id < 0)
        {
            throw new RegraNegocioException("O identificador do funcionário é inválido.");
        }

        Id = id;
        Ativo = ativo;
        AtualizarInformacoes(nome, cpf, email, telefone, endereco, salario, nivelAcesso);
    }

    public abstract bool PodeExecutar(Operacao operacao); //subclasses devem implementar o método com suas próprias caracteísticas
    protected abstract int NivelAcessoPadrao { get; }

    public virtual void AtualizarInformacoes(
        string nome,
        string cpf,
        string email,
        string telefone,
        string endereco,
        double salario,
        int nivelAcesso)
    {
        if (string.IsNullOrWhiteSpace(nome)) //inserção de validações para os campos se alguma não for cumprida vai lançar uma exceção
        {
            throw new RegraNegocioException("Informe o nome do funcionário.");
        }

        if (string.IsNullOrWhiteSpace(cpf))
        {
            throw new RegraNegocioException("Informe o CPF do funcionário.");
        }

        var cpfNormalizado = cpf.Trim().Replace(".", string.Empty).Replace("-", string.Empty);
        if (cpfNormalizado.Length != 11 || !cpfNormalizado.All(char.IsAsciiDigit))
        {
            throw new RegraNegocioException("Informe um CPF com 11 dígitos.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new RegraNegocioException("Informe o e-mail do funcionário.");
        }

        if (string.IsNullOrWhiteSpace(telefone))
        {
            throw new RegraNegocioException("Informe o telefone do funcionário.");
        }

        if (string.IsNullOrWhiteSpace(endereco))
        {
            throw new RegraNegocioException("Informe o endereço do funcionário.");
        }

        if (double.IsNaN(salario) || double.IsInfinity(salario) || salario < 0)
        {
            throw new RegraNegocioException("O salário não pode ser negativo.");
        }

        if (nivelAcesso != NivelAcessoPadrao)
        {
            throw new RegraNegocioException("O nível de acesso informado não é compatível com o cargo do funcionário.");
        }

        Nome = nome.Trim(); //atribuindo valor aos campos
        Cpf = cpfNormalizado;
        Email = email.Trim();
        Telefone = telefone.Trim();
        Endereco = endereco.Trim();
        Salario = salario;
        NivelAcesso = nivelAcesso;
    }

    public void Desativar() //métodos para desativar ou reativar funcionário
    {
        if (!Ativo)
        {
            throw new RegraNegocioException("O funcionário já está desativado.");
        }

        Ativo = false;
    }

    public void Reativar()
    {
        if (Ativo)
        {
            throw new RegraNegocioException("O funcionário já está ativo.");
        }

        Ativo = true;
    }
}
