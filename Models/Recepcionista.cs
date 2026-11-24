using VetCare.Exceptions;
using VetCare.Models.Enums;

namespace VetCare.Models;

public class Recepcionista : Funcionario //herda de funcionario
{
    public string Turno { get; private set; } = string.Empty; //específico da subclasse recepcionista

    public Recepcionista( //seu constrututor igual à superclasse + Turno
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

    public Recepcionista( //sobrecarga para criar um novo recepccionista sem id
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

    protected override int NivelAcessoPadrao => 1; //nivel de acesso da subclasse

    public override bool PodeExecutar(Operacao operacao) => operacao == Operacao.AcessarRecepcao;
    //método abstrato sobrescrito na subclasse pra definir as operações que o r. pode executar
    public void AtualizarInformacoes( //sobrescreve o método da classe pai pra incluir turno
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

    private void AtualizarTurno(string turno) //dispara a exceção se o turno nn for informado
    {
        if (string.IsNullOrWhiteSpace(turno))
        {
            throw new RegraNegocioException("Informe o turno do recepcionista.");
        }

        Turno = turno.Trim();
    }
}
