# Banco de dados do VetCare

O VetCare usa SQLite. O [script.sql](script.sql) cria as tabelas `Tutores`, `Especies`, `Racas`, `Pets` e `Funcionarios` e inclui de 5 a 8 registros fictícios por tabela.

## Inicialização automática

Na raiz do projeto, execute:

```powershell
dotnet run
```

A aplicação lê `ConnectionStrings:DefaultConnection` do [appsettings.json](../appsettings.json). O caminho padrão é `Database/vetcare.db`, relativo à raiz da aplicação. O arquivo do banco fica fora do Git, e as conexões da aplicação têm as chaves estrangeiras habilitadas.

- **Banco novo:** a aplicação executa o script em uma transação e insere os dados iniciais. O comando `sqlite3` é opcional.
- **Banco existente:** a aplicação verifica se as cinco tabelas e suas colunas obrigatórias estão presentes. Ela mantém os dados e interrompe a inicialização com uma mensagem de erro se a estrutura estiver incompleta. Alterações futuras no script precisam ser aplicadas separadamente aos bancos existentes.

## Conferir o script manualmente

Com o utilitário `sqlite3` instalado, execute na raiz do projeto para testar o script em memória. O banco de teste é descartado ao fechar o comando.

```powershell
sqlite3 -bail ":memory:" ".read Database/script.sql" ".tables"
```
