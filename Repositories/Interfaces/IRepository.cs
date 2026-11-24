namespace VetCare.Repositories.Interfaces;

public interface IRepository<T> where T : class //interface genérica para os repositórios
//o T é um tipo genérico que representa a entidade que será manipulada no repositório
{
    void Adicionar(T entidade);
    T? ObterPorId(int id);
    List<T> Listar();
    void Atualizar(T entidade);
    void Excluir(int id); //métodos comuns a todos os repositórios
}