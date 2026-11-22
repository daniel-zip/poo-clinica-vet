# Database (SQLite)

Este diretório contém o script de criação e carga inicial do banco do VetCare.

## Arquivo principal

- `script.sql`: define as tabelas `Tutores`, `Especies`, `Racas`, `Pets` e `Funcionarios`, declara as chaves estrangeiras confirmadas pelo código existente e insere dados fictícios iniciais (5 a 10 por tabela) de forma idempotente.

## Inicialização automática pela aplicação

- A aplicação ASP.NET Core inicializa o banco automaticamente antes de `app.Run()` por meio de `Data/SqliteDatabaseInitializer.cs`, integrado em `Program.cs`.
- O caminho relativo configurado em `ConnectionStrings:DefaultConnection` (padrão: `Data Source=Database/vetcare.db;Foreign Keys=True` em `appsettings.json`) é resolvido a partir do `ContentRootPath` da aplicação, de forma consistente com a `SqliteConnectionFactory` usada pelos repositórios.
- A publicação com `dotnet publish` inclui `Database/script.sql` no mesmo caminho relativo. Execute a aplicação a partir da pasta publicada para usar esse diretório como raiz de conteúdo.
- **Banco novo (arquivo inexistente):** a aplicação lê `Database/script.sql` antes de criar o diretório ou reservar o arquivo do banco. Se a leitura falhar, a partida informa o erro sem criar o banco. Com o script disponível, o esquema e os dados fictícios iniciais são aplicados em uma única transação SQLite.
- **Banco existente com estrutura esperada:** a aplicação confere, em modo somente leitura, as cinco tabelas e suas colunas obrigatórias. A partida independe dos registros presentes. Exclusões legítimas e dados adicionados são preservados.
- **Banco existente vazio ou com estrutura incompleta:** a aplicação informa quais tabelas ou colunas estão ausentes. O banco existente é preservado para revisão manual.
- **Falha na criação inicial:** a transação reverte as alterações no esquema e nos dados. O arquivo SQLite pode permanecer vazio no disco. Se isso ocorrer, uma nova partida apontará a estrutura incompleta até a revisão manual do arquivo.
- A inicialização não executa `DROP TABLE`/`DELETE`, não altera `script.sql` e não cria a tabela `Consultas`.

## Como executar manualmente em um banco temporário (exemplo)

Execute fora dos arquivos persistentes do projeto para validação:

```bash
sqlite3 /caminho/temporario/vetcare-temp.db < Database/script.sql
```

No Windows (PowerShell), usando um caminho temporário:

```powershell
sqlite3 "$env:TEMP\vetcare-temp.db" ".read Database/script.sql"
```

> Se o utilitário `sqlite3` não estiver instalado, o script pode ser aplicado por outra ferramenta SQLite compatível. O projeto utiliza `Microsoft.Data.Sqlite`/Dapper.

## Idempotência do script

- As tabelas usam `CREATE TABLE IF NOT EXISTS`.
- Os registros fictícios são inseridos com `INSERT ... SELECT ... WHERE NOT EXISTS (...)` por `Id`, permitindo executar o script mais de uma vez sem duplicar os seeds (quando aplicado manualmente em um cenário controlado).

## Observações e pendências

- A tabela `Consultas` **não** está definida neste script porque depende do contrato definitivo da Pessoa 3.
- `Repositories/Dapper/PetRepository.cs` consulta `Consultas` (`PetId`) ao excluir um pet; portanto, a exclusão de pets continua dependente da implementação/esquema de `Consultas` e não foi contornada nesta etapa.
- Enquanto `Consultas` estiver pendente, o banco não deve ser declarado completo.
- O script utiliza apenas dados fictícios. Não sobrescreva bancos existentes com dados reais.