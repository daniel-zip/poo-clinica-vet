# Banco de dados do VetCare

O VetCare usa SQLite. O [script.sql](script.sql) cria as tabelas `Tutores`, `Especies`, `Racas`, `Pets`, `Funcionarios`, `Consultas` e `SugestoesCatalogo`. O script inclui de 5 a 8 registros fictícios por tabela, com cinco sugestões de catálogo para demonstrar a fila em bancos novos.

Cada consulta referencia um pet e um funcionário. O índice único parcial `UX_Consultas_Veterinario_DataHora_Agendada` impede que o mesmo veterinário tenha duas consultas agendadas (`Status = 0`) no mesmo horário. Consultas canceladas (`1`) e finalizadas (`2`) ficam no histórico e não ocupam esse horário. O repositório usa `yyyy-MM-dd HH:mm:ss` para gravar e ler `DataHora`.

`SugestoesCatalogo` guarda as sugestões pendentes. Uma sugestão de espécie tem `EspecieId` nulo. Uma sugestão de raça referencia uma espécie existente. A aprovação remove a sugestão da fila e cadastra o item no catálogo quando necessário.

## Inicialização automática

Na raiz do projeto, execute:

```powershell
dotnet run
```

A aplicação lê `ConnectionStrings:DefaultConnection` do [appsettings.json](../appsettings.json). O caminho padrão é `Database/vetcare.db`, relativo à raiz da aplicação. O arquivo do banco fica fora do Git, e as conexões da aplicação têm as chaves estrangeiras habilitadas.

- **Banco novo.** A aplicação cria as sete tabelas, o índice e os dados fictícios em uma única transação. O comando `sqlite3` é opcional.
- **Banco legado com cinco tabelas.** A aplicação cria `Consultas`, seu índice e `SugestoesCatalogo` em uma transação. As duas novas tabelas começam vazias.
- **Banco com seis tabelas e sem fila.** A aplicação cria `SugestoesCatalogo` vazia em uma transação.
- **Banco já atualizado.** A aplicação valida o esquema e preserva as sugestões existentes.
- **Banco inconsistente.** A inicialização informa o problema e interrompe a execução. Se a criação ou a migração falhar, a transação é revertida.

As migrações usam apenas os blocos de criação das tabelas ausentes. Os registros existentes e as exclusões anteriores são preservados.

## Limitações

A criação e as migrações exigem uma conexão gravável. Um banco configurado em modo somente leitura precisa estar atualizado antes da inicialização. Estruturas incompatíveis exigem correção separada.

A FK de `VeterinarioId` verifica se o funcionário existe. O agendamento e o reagendamento exigem um veterinário ativo. A desativação de um veterinário é bloqueada enquanto houver consultas futuras agendadas em seu nome. Essas consultas podem ser reagendadas com outro profissional ou canceladas. As consultas anteriores permanecem disponíveis na agenda geral e no histórico do pet.

## Conferir o script manualmente

Com o utilitário `sqlite3` instalado, execute na raiz do projeto para inspecionar o esquema em memória. O script completo inclui seeds e é destinado a bancos novos. Para migrar um banco legado, use a inicialização automática.

```powershell
sqlite3 -bail ":memory:" ".read Database/script.sql" ".tables" "PRAGMA foreign_key_list('Consultas');" "PRAGMA index_list('Consultas');" "PRAGMA foreign_key_list('SugestoesCatalogo');"
```
