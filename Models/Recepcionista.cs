using VetCare.Exceptions;
using VetCare.Models.Enums;

namespace VetCare.Models;

public class Recepcionista : Funcionario
{
    public string Turno { get; private set; } = string.Empty;

    public Recepcionista(
        int id,
        string nome,
        string cpf,
        string email,
        string telefone,
        string endereco,
        double salario,
        int nivelAcesso,
        string turno,
        bool ativo = true)
        : base(id, nome, cpf, email, telefone, endereco, salario, nivelAcesso, ativo)
    {
        AtualizarTurno(turno);
    }

    public Recepcionista(
        string nome,
        string cpf,
        string email,
        string telefone,
        string endereco,
        double salario,
        int nivelAcesso,
        string turno)
        : this(0, nome, cpf, email, telefone, endereco, salario, nivelAcesso, turno)
    {
    }

    protected override int NivelAcessoPadrao => 1;

    public override bool PodeExecutar(Operacao operacao) => operacao == Operacao.AcessarRecepcao;

    public void AtualizarInformacoes(
        string nome,
        string cpf,
        string email,
        string telefone,
        string endereco,
        double salario,
        int nivelAcesso,
        string turno)
    {
        base.AtualizarInformacoes(nome, cpf, email, telefone, endereco, salario, nivelAcesso);
        AtualizarTurno(turno);
    }

    private void AtualizarTurno(string turno)
    {
        if (string.IsNullOrWhiteSpace(turno))
        {
            throw new RegraNegocioException("Informe o turno do recepcionista.");
        }

        Turno = turno.Trim();
    }
}
