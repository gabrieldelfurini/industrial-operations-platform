# Guia de estudo — Industrial Operations Platform (Item 1)

> Objetivo do item no roadmap: *"Construir uma API com ASP.NET Core Web API, Entity Framework Core,
> PostgreSQL, migrations, validação de dados e tratamento global de exceções."*
>
> Este documento explica **o que foi feito, por que, e como defender cada decisão numa entrevista**.

---

## 1. Visão geral em 30 segundos (o "elevator pitch")

> "É uma API de operações industriais no estilo MES. Ela gerencia produtos, centros de trabalho e
> ordens de produção. A ordem tem um ciclo de vida controlado pelo próprio domínio (Planned → Released
> → InProgress → Completed/Cancelled) e recebe apontamentos de produção com quantidade boa e refugo.
> Usei ASP.NET Core 10, EF Core 10 e PostgreSQL, numa Clean Architecture leve de 4 projetos. A validação
> de entrada é feita com FluentValidation, e todos os erros saem num formato padrão (Problem Details,
> RFC 9457) a partir de um único handler global de exceções. Também tratei concorrência otimista com
> o `xmin` do Postgres, para que dois apontamentos simultâneos não se sobrescrevam."

---

## 2. Arquitetura

```
IndustrialOps.Api            → Controllers, GlobalExceptionHandler, Program.cs (composition root)
   │ referencia
   ▼
IndustrialOps.Application    → Services (casos de uso), DTOs, Validators, interfaces de repositório
   │ referencia                  ▲
   ▼                             │ implementa as interfaces
IndustrialOps.Domain         IndustrialOps.Infrastructure → DbContext, mapeamentos EF, Migrations, Repositórios
(sem dependências)
```

**Regra de ouro: as dependências apontam para dentro.** O Domain não conhece ninguém. O Application
conhece só o Domain. A Infrastructure *implementa* as interfaces que o Application definiu
(**Dependency Inversion**, o "D" do SOLID). A Api junta tudo (composition root).

| Camada | Responsabilidade | Exemplo no código |
|---|---|---|
| **Domain** | Regras de negócio puras, invariantes, estados | `ProductionOrder.Start()` só funciona se a ordem estiver `Released` |
| **Application** | Orquestrar o caso de uso: validar → carregar → chamar o domínio → salvar | `ProductionOrderService.CreateAsync` |
| **Infrastructure** | Detalhes técnicos: banco, EF Core, SQL | `ProductionOrderRepository`, `UnitOfWork`, `AppDbContext` |
| **Api** | HTTP: rotas, status codes, serialização, tratamento de erros | `ProductionOrdersController`, `GlobalExceptionHandler` |

**Por que "leve"?** Não usei MediatR, CQRS ou AutoMapper. Para um projeto deste tamanho eles trariam
mais cerimônia do que benefício. Isso também é uma resposta boa de entrevista: *"escolhi a menor
arquitetura que resolve o problema e que permite evoluir"*.

---

## 3. O caminho de uma requisição (decore este fluxo)

Exemplo: `POST /api/production-orders/{id}/reports` com `{ "goodQuantity": 480, "scrapQuantity": 12.5 }`

1. **Kestrel** recebe a requisição e ela passa pelos middlewares de `Program.cs`, na ordem.
   O primeiro é o `UseExceptionHandler()`, que "envolve" todo o resto.
2. O **roteamento** encontra `ProductionOrdersController.ReportProduction`. O `[ApiController]` faz o
   *model binding*: converte o JSON em `ReportProductionRequest`. Se o JSON estiver quebrado, devolve
   400 sozinho.
3. O controller **só delega**: chama `service.ReportProductionAsync(id, request, ct)`.
4. O **service** (Application):
   - valida a entrada com `reportValidator.ValidateAndThrowAsync`; se inválida, lança `ValidationException`;
   - carrega a ordem pelo repositório; se não existir, lança `NotFoundException`;
   - chama a regra de negócio `order.ReportProduction(...)`; se a ordem não estiver `InProgress`, o
     domínio lança `DomainException`;
   - chama `unitOfWork.SaveChangesAsync()`.
5. O **EF Core** gera um `INSERT` em `production_reports` e um `UPDATE` em `production_orders`
   **na mesma transação**. O `UPDATE` tem `WHERE xmin = <versão lida>`: se outra requisição alterou a
   ordem nesse meio-tempo, nenhuma linha é afetada, o EF lança `DbUpdateConcurrencyException` e o
   `UnitOfWork` a converte em `ConflictException`.
6. Se tudo der certo, o controller devolve **201 Created**. Se qualquer exceção subir, o
   **GlobalExceptionHandler** a transforma no status HTTP certo, em formato Problem Details.

---

## 4. Decisões técnicas e como defendê-las

### 4.1 Domínio rico (e não "anêmico")
- As entidades têm **setters privados** e **métodos com significado de negócio** (`Release`, `Start`,
  `ReportProduction`, `Complete`, `Cancel`). Ninguém consegue fazer `order.Status = Completed` de fora.
- **Máquina de estados dentro da entidade**: a regra "só pode iniciar uma ordem liberada" vive em um
  único lugar. Se amanhã surgir um consumidor de fila (RabbitMQ, item do roadmap) que também inicia
  ordens, a regra continua garantida.
- `ProductionOrder` é um **aggregate root**: os apontamentos (`ProductionReport`) só são criados por
  ela (construtor `internal`), e é ela que mantém `ProducedQuantity`/`ScrapQuantity` consistentes.
- **Tempo injetado** (`DateTime utcNow` como parâmetro, `TimeProvider` no service): deixa o domínio
  determinístico e fácil de testar (item de testes do roadmap).

### 4.2 IDs gerados na aplicação (UUID v7)
- `Guid.CreateVersion7()`: a entidade já tem Id antes de ir para o banco.
- **v7 e não v4**: o v7 é ordenado no tempo, o que fragmenta muito menos o índice B-tree da PK do que
  GUIDs aleatórios.
- **Pegadinha do EF que resolvi**: com Id já preenchido, o EF achava que um `ProductionReport` novo
  adicionado à coleção era um registro existente e fazia `UPDATE`. A solução foi
  `ValueGeneratedNever()` para todas as entidades (veja `AppDbContext.OnModelCreating`).

### 4.3 Número da ordem com SEQUENCE do PostgreSQL
- `PO-000001`, `PO-000002`... vêm de `nextval('production_order_number_seq')`.
- **Por que não `MAX(numero) + 1`?** Porque duas requisições simultâneas leriam o mesmo MAX e gerariam
  números duplicados. A sequence é atômica e segura para concorrência.
- Trade-off: sequences podem ter "buracos" (se a transação falhar, o número é perdido). Para número de
  ordem isso é aceitável.

### 4.4 EF Core: mapeamento com Fluent API (e não Data Annotations)
- Cada entidade tem uma classe `IEntityTypeConfiguration<T>` (pasta `Configurations`). Assim o
  **Domain fica livre de atributos de banco**.
- Detalhes que valem citar:
  - `HasMaxLength`: vira `varchar(n)`, nada de `text` sem limite.
  - `HasPrecision(18, 3)` em quantidades: `decimal` exato (nunca `float` para quantidade ou dinheiro).
  - Índices **únicos** em `Code` e `OrderNumber`, mais um índice em `Status` (usado no filtro da listagem).
  - `OnDelete(Restrict)` para produto e centro de trabalho: não dá para apagar um produto que tem
    ordens. `Cascade` para apontamentos, que pertencem à ordem.
  - Enum `Status` salvo como **texto**: legível no SQL e não quebra se a ordem do enum mudar.
  - **snake_case** (`EFCore.NamingConventions`): padrão do PostgreSQL, que evita ter que usar aspas em
    `"PlannedQuantity"`.
- `AsNoTracking()` nas consultas de leitura: mais rápido, porque o EF não guarda cópia para detectar mudanças.

### 4.5 Repository + Unit of Work
- Os **repositórios** só *preparam* mudanças (`Add`) e fazem consultas. Quem decide **quando** salvar é
  o service, via `IUnitOfWork.SaveChangesAsync()`. Assim, um caso de uso que altera várias entidades
  salva tudo numa transação só.
- "Mas o `DbContext` já é um Unit of Work e o `DbSet` já é um repositório?" Sim, e essa é uma pergunta
  clássica. A resposta: as interfaces existem para **o Application não depender de EF Core**
  (testabilidade com mocks/NSubstitute e troca de tecnologia) e para **traduzir erros de banco**
  (unique violation e concorrência viram `ConflictException`) num único lugar.

### 4.6 Concorrência otimista com `xmin`
- O PostgreSQL tem uma coluna de sistema `xmin` em toda tabela, que muda a cada `UPDATE`. Ela foi
  mapeada como *row version* numa **shadow property** (o domínio nem sabe que ela existe).
- **Testado**: 10 apontamentos simultâneos na mesma ordem resultaram em 9 × 201 e 1 × 409, com o total
  produzido exatamente igual à soma dos apontamentos salvos. Sem isso haveria *lost update*: duas
  requisições leem `Produced = 10`, ambas somam 1 e ambas gravam 11.
- **Otimista vs pessimista**: otimista não trava linhas (sem `SELECT ... FOR UPDATE`) e assume que o
  conflito é raro. Quando acontece, o cliente recebe 409 e tenta de novo.

### 4.7 Migrations
- `InitialCreate`: cria tabelas, índices, FKs e a sequence.
- `SeedReferenceData`: insere produtos e centros de trabalho de exemplo com `HasData` (valores fixos;
  se usasse `Guid.NewGuid()`, o EF detectaria "mudança" a cada nova migration).
- A tabela `__EFMigrationsHistory` registra quais migrations já foram aplicadas.
- **Em desenvolvimento** a API aplica as migrations ao subir (`Database:ApplyMigrationsOnStartup`).
  **Em produção isso fica desligado**: o recomendado é aplicar via pipeline (script SQL idempotente
  ou *migration bundle*), para não ter várias instâncias migrando ao mesmo tempo e porque o usuário da
  aplicação não deveria ter permissão de DDL. Isso volta no item de CI/CD.
- Os comandos usam `--project` (onde ficam as migrations: Infrastructure) e `--startup-project` (de onde
  vêm a configuração e a connection string: Api).

### 4.8 Validação de dados: FluentValidation
- **Por que não Data Annotations (`[Required]`)?** As regras ficam separadas dos DTOs, são testáveis
  isoladamente, suportam regras entre campos (`PlannedEnd > PlannedStart`) e são mais legíveis.
- **Onde valida?** No *service* (`ValidateAndThrowAsync`), e não num filtro mágico do MVC. Assim a
  validação vale para qualquer porta de entrada (HTTP hoje, fila amanhã).
- Desliguei o `[Required]` implícito do ASP.NET
  (`SuppressImplicitRequiredAttributeForNonNullableReferenceTypes`) para haver **uma única fonte de
  verdade** de validação.
- **Três níveis de proteção** (vale explicar):
  1. **Formato** (FluentValidation): campo vazio, tamanho, número > 0, datas → 400.
  2. **Referências** (service): `productId` que não existe → 400 com erro no campo `productId`.
     Não é 404, porque o recurso da URL existe; o que está errado é o corpo da requisição.
  3. **Regras de negócio** (domínio): transição de estado inválida, produto inativo → 422.
     O domínio também tem *guard clauses* como última linha de defesa.
- Datas chegam como `DateTimeOffset` (exigindo fuso explícito) e são normalizadas para UTC. Testado:
  `17:00-03:00` foi salvo como `20:00Z`. No banco a coluna é `timestamp with time zone`.

### 4.9 Tratamento global de exceções
- **Um único lugar** (`GlobalExceptionHandler : IExceptionHandler`) converte exceção em HTTP. Os
  controllers e services **não têm try/catch** de HTTP; eles só lançam exceções com significado.
- Todas as respostas de erro seguem o **RFC 9457 (Problem Details)**, `application/problem+json`, com
  `type`, `title`, `status`, `detail`, `instance` e `traceId`.
- Mapeamento:

  | Exceção | Status | Por quê |
  |---|---|---|
  | `ValidationException` | 400 | Entrada inválida; vem com o dicionário `errors` por campo |
  | `NotFoundException` | 404 | O recurso da URL não existe |
  | `ConflictException` | 409 | Conflita com o estado atual: código duplicado ou edição concorrente |
  | `DomainException` | 422 | A requisição é válida, mas viola uma regra de negócio |
  | qualquer outra | 500 | Mensagem genérica, **sem stack trace nem SQL**; detalhe só no log |

- **Segurança**: o 500 nunca vaza detalhes internos. Testei derrubando a conexão com o banco: o
  cliente recebe só `"An unexpected error occurred."` e o `traceId`; o log do servidor tem o
  `NpgsqlException` completo. O `traceId` liga as duas coisas, e o suporte usa ele para achar o log.
- Também desliguei mensagens de erro de JSON malformado que expunham nomes internos de tipos
  (`AllowInputFormatterExceptionMessages = false`).
- **422 vs 409 para transição inválida** é uma discussão comum; as duas são defensáveis. Usei 409
  para conflitos de *dados* (duplicidade e concorrência) e 422 para *regras de negócio*, o que deixa o
  cliente distinguir "tente de novo" de "isso nunca vai funcionar nesse estado".

### 4.10 Design da API REST
- Recursos no plural e kebab-case: `/api/work-centers`, `/api/production-orders`.
- `POST` de criação devolve **201 Created** com header `Location` (`CreatedAtAction`).
- **Soft delete** (`DELETE` desativa e não apaga): mantém o histórico de ordens que referenciam o
  produto. Também por isso a FK é `Restrict`.
- **Transições como ações** (`POST /{id}/start`) em vez de `PATCH { "status": "InProgress" }`: cada
  transição tem regras e efeitos próprios (o `start` grava `ActualStart`), e a URL deixa explícito o
  que acontece. É o mesmo estilo de APIs como a do Stripe (`/charges/{id}/capture`).
- Enums trafegam como texto (`"InProgress"`), não como número.

### 4.11 Configuração e segredos
- A connection string de desenvolvimento fica em `appsettings.Development.json`, com uma senha **só de
  desenvolvimento local**. Em produção ela vem de variável de ambiente
  (`ConnectionStrings__Database`) ou de um cofre (Azure Key Vault, item de Cloud do roadmap).
- A API falha **logo ao subir** se a connection string não existir (*fail fast*), em vez de falhar na
  primeira requisição.
- O banco tem um **usuário próprio** (`industrialops`), dono só do seu banco; nada de usar o
  superusuário `postgres` (princípio do menor privilégio).

---

## 5. O que ficou de fora de propósito (e onde entra no roadmap)

Saber dizer "não fiz X ainda, e faria assim" é tão importante quanto o que foi feito.

| Ficou de fora | Item do roadmap que cobre |
|---|---|
| Autenticação/autorização (JWT, roles, policies) | Backend: OAuth 2.0 e JWT |
| Paginação, `EXPLAIN ANALYZE`, índices compostos | Backend: otimizar consultas PostgreSQL |
| Versionamento da API, idempotência (`Idempotency-Key`) | Backend: OpenAPI, versionamento, idempotência |
| Testes unitários e de integração | Automated Testing (xUnit, NSubstitute, Testcontainers) |
| Docker da API, health checks, CI/CD | Cloud, DevOps & Production Readiness |
| Logs estruturados e tracing distribuído | OpenTelemetry e Application Insights |

---

## 6. Perguntas prováveis de entrevista (com respostas curtas)

**1. Por que Clean Architecture? Não é exagero para um CRUD?**
Não é só CRUD: existe uma máquina de estados e regras de produção. Separar o domínio deixa essas regras
testáveis sem banco e sem HTTP, e permite trocar a infraestrutura. Mantive "leve" (sem MediatR/CQRS)
para não pagar complexidade que o projeto ainda não precisa.

**2. Qual a diferença entre Domain e Application?**
O Domain diz *o que é permitido* (invariantes, por exemplo "não pode apontar em ordem que não está em
produção"). O Application diz *como executar um caso de uso*: valida a entrada, busca os dados, chama o
domínio e salva. O Domain não sabe que existe banco ou HTTP.

**3. O que acontece se duas pessoas apontarem produção na mesma ordem ao mesmo tempo?**
Concorrência otimista com `xmin`: a segunda gravação detecta que a linha mudou, o EF lança
`DbUpdateConcurrencyException` e a API devolve 409. O total nunca fica inconsistente. Testei com 10
requisições paralelas.

**4. Como você evita que o erro 500 exponha informações sensíveis?**
O handler global devolve uma mensagem genérica com `traceId` e loga a exceção completa no servidor.
Também desliguei as mensagens detalhadas de JSON inválido.

**5. Por que validar no service e não só no controller?**
Porque o service é a porta de entrada dos casos de uso. Se amanhã o mesmo caso de uso for disparado
por uma mensagem do RabbitMQ, a validação continua valendo.

**6. Validação vs regra de negócio: qual a diferença?**
Validação é "o dado está bem formado?" (campo obrigatório, número positivo) → 400. Regra de negócio
depende do estado atual ("a ordem já está concluída") → 422.

**7. Como funcionam migrations? Como você aplicaria em produção?**
Cada migration é uma classe C# com `Up`/`Down`, gerada comparando o modelo atual com o *snapshot*. O
EF registra as aplicadas em `__EFMigrationsHistory`. Em produção, eu geraria um script idempotente
(`dotnet ef migrations script --idempotent`) ou um bundle e aplicaria pelo pipeline de CI/CD, com um
usuário que tenha permissão de DDL, e não automaticamente no startup.

**8. `AsNoTracking`: quando usar?**
Em consultas só de leitura. O EF não cria snapshot para detectar mudanças, o que economiza memória e
CPU. Quando vou alterar a entidade e salvar, uso a consulta rastreada (tracked).

**9. Por que `decimal` e não `double` nas quantidades?**
`double` é ponto flutuante binário: `0.1 + 0.2 != 0.3`. Quantidades industriais (kg, litros) e
dinheiro precisam de exatidão, por isso `decimal` / `numeric(18,3)`.

**10. Por que soft delete?**
Ordens antigas referenciam o produto; apagar fisicamente quebraria o histórico e a rastreabilidade,
que é essencial em MES. Desativar impede o uso em novas ordens e preserva o passado.

**11. Por que UUID v7 e não int autoincrement?**
O Id é gerado na aplicação, sem ida ao banco, e não expõe quantos registros existem (um id sequencial é
enumerável). O v7 é ordenado no tempo, então não fragmenta o índice como o v4. O número legível para
humanos é o `OrderNumber`, que vem de uma sequence.

**12. O que você melhoraria a seguir?**
Testes automatizados, paginação na listagem de ordens, autenticação com roles (por exemplo, só
supervisor cancela uma ordem), idempotência no apontamento (para evitar duplicidade em retry de
rede) e observabilidade com OpenTelemetry.

---

## 7. Mapa dos arquivos mais importantes

| Arquivo | O que olhar |
|---|---|
| `src/IndustrialOps.Domain/ProductionOrders/ProductionOrder.cs` | Máquina de estados e regras |
| `src/IndustrialOps.Application/ProductionOrders/ProductionOrderService.cs` | Orquestração de casos de uso |
| `src/IndustrialOps.Application/ProductionOrders/ProductionOrderValidators.cs` | Regras de validação |
| `src/IndustrialOps.Infrastructure/Persistence/AppDbContext.cs` | Sequence, `ValueGeneratedNever` |
| `src/IndustrialOps.Infrastructure/Persistence/Configurations/ProductionOrderConfiguration.cs` | Mapeamento, índices, `xmin` |
| `src/IndustrialOps.Infrastructure/Persistence/UnitOfWork.cs` | Tradução de erros de banco |
| `src/IndustrialOps.Infrastructure/Persistence/Migrations/` | As duas migrations |
| `src/IndustrialOps.Api/ErrorHandling/GlobalExceptionHandler.cs` | Exceção → HTTP |
| `src/IndustrialOps.Api/Program.cs` | DI e pipeline de middlewares |
| `src/IndustrialOps.Api/IndustrialOps.http` | Roteiro de testes manuais |
