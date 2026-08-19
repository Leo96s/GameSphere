# Quiz MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Entregar um fluxo autenticado de catálogo, realização e resultado de quizzes curados, com administração por role e testes automatizados contra PostgreSQL temporário.

**Architecture:** O backend mantém a organização existente por controllers, services, DTOs e mappers. `QuizCatalogService` expõe apenas contratos seguros para participantes; `QuizAdministrationService` concentra alterações de conteúdo protegidas por role; `InitialAdminBootstrapper` trata exclusivamente do administrador inicial. O frontend Vue mantém uma única SPA, com rotas protegidas e serviços Axios por domínio.

**Tech Stack:** .NET 10, ASP.NET Core, EF Core 10.0.2, Npgsql, PostgreSQL 17, Vue 3, Vite 7, Axios, Vitest 4, xUnit v3.2.2, `Microsoft.AspNetCore.Mvc.Testing` 10.0.2, `Microsoft.NET.Test.Sdk` 18.8.1 e `Testcontainers.PostgreSql` 4.13.0.

## Global Constraints

- Landing e destaques são públicos; catálogo, quiz, resultado e administração exigem JWT.
- Não guardar, imprimir, testar ou versionar valores reais de `INITIAL_ADMIN_EMAIL`, `INITIAL_ADMIN_PASSWORD`, SMTP, JWT ou connection strings.
- `INITIAL_ADMIN_EMAIL` e `INITIAL_ADMIN_PASSWORD` são obrigatórias para o bootstrap; em produção chegam por gestor de segredos, CI ou ficheiro externo ao repositório.
- A role persistente é `User` ou `Admin`; endpoints administrativos devolvem `403` a utilizadores autenticados sem `Admin`.
- Os contratos de participante nunca incluem `CorrectAnswer`; a pontuação é calculada no servidor.
- Uma tentativa contém exatamente uma resposta válida por pergunta; cria um `Score` com `QuizzId`, `GameId = null`, data UTC e pontos iguais aos acertos.
- Não implementar rankings, XP, gestão de utilizadores, uploads, quizzes de utilizadores, estatísticas ou limites de tentativas.
- Os novos pacotes de teste são mantidos e compatíveis com .NET 10; rever `dotnet list package --vulnerable` antes do commit que os introduz.

## Estrutura de ficheiros

- `GameSphere_backend/Enums/UserRole.cs` — valores persistentes de autorização.
- `GameSphere_backend/Services/InitialAdminBootstrapper.cs` — cria ou promove idempotentemente o administrador configurado.
- `GameSphere_backend/Interfaces/IQuizCatalogService.cs` e `Services/QuizCatalogService.cs` — leitura autenticada e submissão de tentativas.
- `GameSphere_backend/Interfaces/IQuizAdministrationService.cs` e `Services/QuizAdministrationService.cs` — CRUD transacional de quizzes e perguntas para administradores.
- `GameSphere_backend/Models/FrontendModels/Quiz*.cs` e `Admin*.cs` — contratos separados para participante, resultado e administração.
- `GameSphere_backend/Mappers/QuizPlayerMapper.cs` — mapeia perguntas sem revelar a resposta correta.
- `GameSphere_backend/Controllers/QuizzesController.cs`, `AdminQuizzesController.cs`, `AdminQuestionsController.cs` — limites HTTP para cada responsabilidade.
- `GameSphere_backend.Tests/` — testes xUnit com PostgreSQL efémero e host ASP.NET Core.
- `GameSphere_frontend/src/services/quizService.js` e `adminQuizService.js` — chamadas Axios por domínio.
- `GameSphere_frontend/src/views/Quizzes/` e `src/views/Admin/` — catálogo, execução, resultado e gestão de conteúdo.
- `GameSphere_frontend/src/composables/useQuizAttempt.js` — estado da tentativa, sem regras de pontuação.
- `GameSphere_frontend/src/**/*.spec.js` — testes Vitest de guard, serviços e composable.

---

### Task 1: Criar a fundação de testes do backend

**Files:**
- Create: `GameSphere_backend.Tests/GameSphere_backend.Tests.csproj`
- Create: `GameSphere_backend.Tests/Infrastructure/GameSphereApiFactory.cs`
- Create: `GameSphere_backend.Tests/Infrastructure/PostgreSqlFixture.cs`
- Modify: `GameSphere_backend/GameSphere_backend.sln`
- Modify: `GameSphere_backend/Program.cs`

**Interfaces:**
- Produces `GameSphereApiFactory : WebApplicationFactory<Program>` e `PostgreSqlFixture : IAsyncLifetime` para os testes seguintes.
- Produces `public partial class Program { }` para tornar a entry point acessível ao host de teste.

- [ ] **Step 1: Criar o projeto de testes com referências explícitas.**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework><IsPackable>false</IsPackable></PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.8.1" />
    <PackageReference Include="xunit.v3" Version="3.2.2" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.2" />
    <PackageReference Include="Testcontainers.PostgreSql" Version="4.13.0" />
  </ItemGroup>
  <ItemGroup><ProjectReference Include="..\GameSphere_backend\GameSphere_backend.csproj" /></ItemGroup>
</Project>
```

- [ ] **Step 2: Implementar `PostgreSqlFixture` com `PostgreSqlBuilder("postgres:17-alpine")`, iniciar o contentor em `InitializeAsync`, aplicar `Database.MigrateAsync()` e parar/eliminar o contentor em `DisposeAsync`.**

- [ ] **Step 3: Implementar `GameSphereApiFactory` para substituir `ConnectionStrings:GameSphereDB` pela connection string do fixture e injetar valores fictícios para JWT, email e bootstrap admin.**

```csharp
builder.UseSetting("ConnectionStrings:GameSphereDB", _database.ConnectionString);
builder.UseSetting("JwtSettings:SecretKey", "test-secret-with-at-least-thirty-two-bytes");
builder.UseSetting("InitialAdmin:Email", "admin@example.test");
builder.UseSetting("InitialAdmin:Password", "Test-only-admin-password-123");
```

- [ ] **Step 4: Adicionar o projeto à solution e executar o teste inicial de arranque.**

```csharp
[Fact]
public async Task Swagger_is_available_in_development()
{
    var response = await _client.GetAsync("/swagger/v1/swagger.json");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

Run: `dotnet test GameSphere_backend.Tests/GameSphere_backend.Tests.csproj`
Expected: PASS com Docker disponível; falha clara se o daemon Docker não estiver disponível.

- [ ] **Step 5: Rever vulnerabilidades e commitar.**

Run: `dotnet list GameSphere_backend.Tests/GameSphere_backend.Tests.csproj package --vulnerable`
Commit: `test(api): add PostgreSQL integration harness`

### Task 2: Persistir roles e bootstrap seguro do administrador

**Files:**
- Create: `GameSphere_backend/Enums/UserRole.cs`
- Create: `GameSphere_backend/Services/InitialAdminBootstrapper.cs`
- Create: `GameSphere_backend.Tests/Authentication/InitialAdminBootstrapperTests.cs`
- Modify: `GameSphere_backend/Models/BackendModels/User.cs`
- Modify: `GameSphere_backend/Models/FrontendModels/UserDto.cs`
- Modify: `GameSphere_backend/Mappers/UserMapper.cs`
- Modify: `GameSphere_backend/Program.cs`
- Create: `GameSphere_backend/Migrations/<timestamp>_AddUserRole.cs`
- Create: `GameSphere_backend/Migrations/<timestamp>_AddUserRole.Designer.cs`
- Modify: `GameSphere_backend/Migrations/AppDbContextModelSnapshot.cs`
- Modify: `.env.example`, `compose.yml`, `compose.dev.yml`, `compose.prod.yml`, `README.md`

**Interfaces:**
- Produces `UserRole { User, Admin }` e `User.Role` com valor por defeito `User`.
- Produces `Task EnsureAdminAsync(CancellationToken cancellationToken = default)`.

- [ ] **Step 1: Escrever testes que falham para criação, promoção e idempotência.**

```csharp
[Fact]
public async Task EnsureAdminAsync_promotes_existing_configured_user()
{
    await SeedUserAsync("admin@example.test", UserRole.User);
    await _bootstrapper.EnsureAdminAsync();
    Assert.Equal(UserRole.Admin, await GetRoleAsync("admin@example.test"));
}
```

- [ ] **Step 2: Adicionar `UserRole` e a propriedade `public UserRole Role { get; set; } = UserRole.User;`; propagar `Role` para `UserDto` e `UserMapper`.**

- [ ] **Step 3: Implementar o bootstrap num serviço scoped. Validar email e password configurados; procurar por email; promover o existente ou criar um utilizador ativo com password BCrypt e campos mínimos válidos; nunca registar a password.**

```csharp
await using var scope = app.Services.CreateAsyncScope();
await scope.ServiceProvider.GetRequiredService<InitialAdminBootstrapper>()
    .EnsureAdminAsync();
```

- [ ] **Step 4: Criar e rever a migração EF. Passar `InitialAdmin__Email` e `InitialAdmin__Password` a partir das variáveis obrigatórias `INITIAL_ADMIN_EMAIL` e `INITIAL_ADMIN_PASSWORD` nos Compose; adicionar apenas placeholders fictícios ao `.env.example`.**

- [ ] **Step 5: Executar os testes do bootstrap e compilar.**

Run: `dotnet test GameSphere_backend.Tests/GameSphere_backend.Tests.csproj --filter InitialAdminBootstrapperTests`
Run: `dotnet build GameSphere_backend/GameSphere_backend.csproj`
Expected: PASS sem segredos no output.

- [ ] **Step 6: Commit.**

Commit: `feat(auth): add persistent admin role`

### Task 3: Incluir roles no JWT e proteger a autorização

**Files:**
- Modify: `GameSphere_backend/Interfaces/IAuthService.cs`
- Modify: `GameSphere_backend/Services/AuthService.cs`
- Modify: `GameSphere_backend/Services/UserServices.cs`
- Modify: `GameSphere_backend/Program.cs`
- Create: `GameSphere_backend.Tests/Authentication/RoleAuthorizationTests.cs`

**Interfaces:**
- Changes `GenerateToken(string userId, string email, UserRole role)`.
- Produces um claim `ClaimTypes.Role` e política JWT que o interpreta como role.

- [ ] **Step 1: Escrever testes para token com role, `401` sem token e `403` para token de `User` num endpoint `[Authorize(Roles = "Admin")]`.**

```csharp
Assert.Contains(new Claim(ClaimTypes.Role, UserRole.Admin.ToString()), claims);
Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
```

- [ ] **Step 2: Alterar interface, `AuthService` e todos os chamadores de login/social login para passarem `user.Role`; configurar `RoleClaimType = ClaimTypes.Role` nos `TokenValidationParameters`.**

- [ ] **Step 3: Executar testes de autenticação.**

Run: `dotnet test GameSphere_backend.Tests/GameSphere_backend.Tests.csproj --filter RoleAuthorizationTests`
Expected: PASS.

- [ ] **Step 4: Commit.**

Commit: `feat(auth): authorize admin roles`

### Task 4: Criar contratos seguros e o catálogo de quizzes

**Files:**
- Create: `GameSphere_backend/Models/FrontendModels/QuizCatalogItemDto.cs`
- Create: `GameSphere_backend/Models/FrontendModels/QuizPlayDto.cs`
- Create: `GameSphere_backend/Models/FrontendModels/QuizQuestionDto.cs`
- Create: `GameSphere_backend/Mappers/QuizPlayerMapper.cs`
- Create: `GameSphere_backend/Interfaces/IQuizCatalogService.cs`
- Create: `GameSphere_backend/Services/QuizCatalogService.cs`
- Create: `GameSphere_backend/Controllers/QuizzesController.cs`
- Create: `GameSphere_backend.Tests/Quizzes/QuizCatalogTests.cs`
- Modify: `GameSphere_backend/Models/BackendModels/Quizz.cs`
- Create: `GameSphere_backend/Migrations/<timestamp>_AddQuizPublication.cs`
- Create: `GameSphere_backend/Migrations/<timestamp>_AddQuizPublication.Designer.cs`
- Modify: `GameSphere_backend/Migrations/AppDbContextModelSnapshot.cs`
- Modify: `GameSphere_backend/Program.cs`

**Interfaces:**
- Produces `Task<IReadOnlyList<QuizCatalogItemDto>> GetPublishedAsync()` and `Task<QuizPlayDto?> GetPublishedByIdAsync(int id)`.
- Produces `GET /api/quizzes` and `GET /api/quizzes/{id}`, ambos `[Authorize]`.

- [ ] **Step 1: Escrever testes de integração que criam um quiz publicado e um rascunho, pedem catálogo autenticado e verificam que só o publicado é devolvido e que o JSON não contém `correctAnswer`.**

```csharp
var body = await response.Content.ReadAsStringAsync();
Assert.DoesNotContain("correctAnswer", body, StringComparison.OrdinalIgnoreCase);
Assert.DoesNotContain(draft.Title, body, StringComparison.Ordinal);
```

- [ ] **Step 2: Adicionar `IsPublished` com valor por defeito `false`, criar a migração e implementar consultas `AsNoTracking()` com `Include(q => q.Questions)` apenas no detalhe.**

- [ ] **Step 3: Implementar `QuizPlayerMapper` sem referência a `Question.CorrectAnswer`; registar `IQuizCatalogService`; devolver `404` para ID inexistente ou não publicado.**

- [ ] **Step 4: Executar testes do catálogo e a migração num PostgreSQL temporário.**

Run: `dotnet test GameSphere_backend.Tests/GameSphere_backend.Tests.csproj --filter QuizCatalogTests`
Expected: PASS.

- [ ] **Step 5: Commit.**

Commit: `feat(quiz): add protected published catalog`

### Task 5: Implementar submissões e resultados calculados no servidor

**Files:**
- Create: `GameSphere_backend/Models/FrontendModels/QuizAnswerRequest.cs`
- Create: `GameSphere_backend/Models/FrontendModels/QuizAttemptRequest.cs`
- Create: `GameSphere_backend/Models/FrontendModels/QuizAttemptResultDto.cs`
- Modify: `GameSphere_backend/Interfaces/IQuizCatalogService.cs`
- Modify: `GameSphere_backend/Services/QuizCatalogService.cs`
- Modify: `GameSphere_backend/Controllers/QuizzesController.cs`
- Create: `GameSphere_backend.Tests/Quizzes/QuizAttemptTests.cs`

**Interfaces:**
- Produces `Task<ServiceResponse<QuizAttemptResultDto>> SubmitAttemptAsync(int quizId, int userId, QuizAttemptRequest request)`.
- Produces `POST /api/quizzes/{id}/attempts`.

- [ ] **Step 1: Escrever testes que falham para tentativa válida, resposta duplicada, pergunta em falta, ID estranho, opção inválida e quiz não publicado.**

```csharp
Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
Assert.Equal(2, result.CorrectAnswers);
Assert.Equal(100m, result.Percentage);
```

- [ ] **Step 2: Implementar a validação no serviço: carregar o quiz com perguntas; comparar o conjunto completo de `QuestionId`; confirmar cada `SelectedAnswer` pertence a `Answers`; calcular acertos pelo `CorrectAnswer`; persistir `Score` com `QuizzId`, `GameId = null`, `Date = DateTime.UtcNow` e `Points = correctAnswers`.**

- [ ] **Step 3: Extrair o ID do utilizador de `ClaimTypes.NameIdentifier`/`sub`, nunca do payload, e devolver apenas `CorrectAnswers`, `TotalQuestions` e `Percentage`.**

- [ ] **Step 4: Executar testes da tentativa.**

Run: `dotnet test GameSphere_backend.Tests/GameSphere_backend.Tests.csproj --filter QuizAttemptTests`
Expected: PASS e uma linha de `Scores` por submissão válida.

- [ ] **Step 5: Commit.**

Commit: `feat(quiz): score authenticated attempts`

### Task 6: Implementar CRUD administrativo de quizzes e perguntas

**Files:**
- Create: `GameSphere_backend/Models/FrontendModels/AdminQuizUpsertDto.cs`
- Create: `GameSphere_backend/Models/FrontendModels/AdminQuestionUpsertDto.cs`
- Create: `GameSphere_backend/Interfaces/IQuizAdministrationService.cs`
- Create: `GameSphere_backend/Services/QuizAdministrationService.cs`
- Create: `GameSphere_backend/Controllers/AdminQuizzesController.cs`
- Create: `GameSphere_backend/Controllers/AdminQuestionsController.cs`
- Create: `GameSphere_backend.Tests/Admin/QuizAdministrationTests.cs`
- Modify: `GameSphere_backend/Program.cs`

**Interfaces:**
- Produces CRUD para quizzes e perguntas em `/api/admin/...`, todos com `[Authorize(Roles = "Admin")]`.
- Produces `Task<ServiceResponse<AdminQuizUpsertDto>> CreateQuizAsync(int adminId, AdminQuizUpsertDto request)` e métodos equivalentes de update/delete.

- [ ] **Step 1: Escrever testes de integração para `401`, `403`, criar/editar/publicar/apagar quiz, criar/editar/apagar pergunta e recalcular `NumberOfQuests`.**

```csharp
Assert.Equal(HttpStatusCode.Forbidden, userResponse.StatusCode);
Assert.Equal(2, updatedQuiz.NumberOfQuests);
```

- [ ] **Step 2: Implementar validação de título, perguntas, opções e resposta correta. Rejeitar `CorrectAnswer` que não exista em `Answers`; definir `UserId` a partir do admin autenticado na criação; recalcular `NumberOfQuests` depois de cada alteração.**

- [ ] **Step 3: Implementar alterações de quiz e respetivas perguntas em transação EF Core quando o pedido as inclui; apagar perguntas antes do quiz para respeitar a FK.**

- [ ] **Step 4: Registar o serviço e executar os testes administrativos.**

Run: `dotnet test GameSphere_backend.Tests/GameSphere_backend.Tests.csproj --filter QuizAdministrationTests`
Expected: PASS.

- [ ] **Step 5: Commit.**

Commit: `feat(admin): manage curated quizzes`

### Task 7: Preparar autenticação e testes no frontend

**Files:**
- Modify: `GameSphere_frontend/package.json`
- Modify: `GameSphere_frontend/src/services/api.js`
- Modify: `GameSphere_frontend/src/services/authService.js`
- Modify: `GameSphere_frontend/src/router.js`
- Create: `GameSphere_frontend/src/router.spec.js`
- Create: `GameSphere_frontend/src/services/api.spec.js`
- Modify: `GameSphere_frontend/src/layout/AppHeader.vue`

**Interfaces:**
- Produces script `"test": "vitest run"`.
- Produces interceptor Axios que envia `Authorization: Bearer <token>` quando existe token local.
- Produces guards `requiresAuth` e `requiresAdmin`.

- [ ] **Step 1: Escrever testes Vitest que falham para redireção sem token, bloqueio de rota admin para `User`, permissão para `Admin` e cabeçalho Bearer.**

```js
expect(await guard({ meta: { requiresAdmin: true } }, { role: 'User' })).toEqual({ name: 'landing' })
expect(request.headers.Authorization).toBe('Bearer test-token')
```

- [ ] **Step 2: Centralizar `getCurrentUser`, `getCurrentRole` e `isAdmin` num utilitário de autenticação; ajustar login/social login para guardar o DTO que contém `role`; remover logs de utilizador/token do fluxo de autenticação.**

- [ ] **Step 3: Adicionar o interceptor em Axios e guards de rota. Mostrar links Quizzes a utilizadores autenticados e Administração apenas a admins, mantendo a landing pública.**

- [ ] **Step 4: Executar testes e build.**

Run: `npm run test`
Run: `npm run build`
Expected: PASS.

- [ ] **Step 5: Commit.**

Commit: `test(frontend): cover authenticated navigation`

### Task 8: Implementar catálogo, execução e resultado no frontend

**Files:**
- Create: `GameSphere_frontend/src/services/quizService.js`
- Create: `GameSphere_frontend/src/composables/useQuizAttempt.js`
- Create: `GameSphere_frontend/src/views/Quizzes/QuizCatalogPage.vue`
- Create: `GameSphere_frontend/src/views/Quizzes/QuizPlayPage.vue`
- Create: `GameSphere_frontend/src/views/Quizzes/QuizResultPage.vue`
- Create: `GameSphere_frontend/src/composables/useQuizAttempt.spec.js`
- Modify: `GameSphere_frontend/src/router.js`

**Interfaces:**
- Produces `listQuizzes()`, `getQuiz(id)` e `submitAttempt(id, answers)`.
- Produces `useQuizAttempt(quiz)` com `selectAnswer(questionId, selectedAnswer)`, `isComplete` e `toRequest()`.

- [ ] **Step 1: Escrever testes Vitest que falham para uma resposta por pergunta, `isComplete`, payload de submissão e limpeza de estado depois de receber o resultado.**

```js
attempt.selectAnswer(4, 'Lisbon')
expect(attempt.toRequest()).toEqual({ answers: [{ questionId: 4, selectedAnswer: 'Lisbon' }] })
```

- [ ] **Step 2: Implementar o serviço Axios e composable sem respostas corretas ou cálculo de pontuação. A página de quiz só ativa submissão quando `isComplete` é verdadeiro.**

- [ ] **Step 3: Implementar catálogo autenticado, vista de jogo com loading/erro e resultado a partir da resposta do servidor; guardar o resultado na navegação por state e redirecionar ao catálogo se não existir resultado.**

- [ ] **Step 4: Executar testes, build e smoke test autenticado com dados fictícios.**

Run: `npm run test`
Run: `npm run build`
Expected: PASS.

- [ ] **Step 5: Commit.**

Commit: `feat(quiz): add player quiz flow`

### Task 9: Implementar a área de administração Vue

**Files:**
- Create: `GameSphere_frontend/src/services/adminQuizService.js`
- Create: `GameSphere_frontend/src/views/Admin/AdminQuizListPage.vue`
- Create: `GameSphere_frontend/src/views/Admin/AdminQuizEditorPage.vue`
- Create: `GameSphere_frontend/src/views/Admin/AdminQuestionEditor.vue`
- Create: `GameSphere_frontend/src/services/adminQuizService.spec.js`
- Modify: `GameSphere_frontend/src/router.js`
- Modify: `GameSphere_frontend/src/layout/AppHeader.vue`

**Interfaces:**
- Produces `listAdminQuizzes`, `createQuiz`, `updateQuiz`, `deleteQuiz`, `createQuestion`, `updateQuestion` e `deleteQuestion`.
- Produces `/admin/quizzes` e `/admin/quizzes/:id`, ambas `requiresAuth` e `requiresAdmin`.

- [ ] **Step 1: Escrever testes Vitest que falham para pedidos administrativos com Bearer, estado de publicação, validação local de resposta correta nas opções e mensagem de erro de `403`.**

```js
expect(validateQuestion({ answers: ['A', 'B'], correctAnswer: 'C' }))
  .toEqual('A resposta correta tem de constar nas opções.')
```

- [ ] **Step 2: Implementar serviço e lista de quizzes com criar, editar, apagar e publicar/despublicar. Apresentar erros da API sem ocultar `403` como sucesso.**

- [ ] **Step 3: Implementar editor de quiz/perguntas. A validação de cliente melhora UX, mas o servidor continua a validar todos os campos.**

- [ ] **Step 4: Executar testes e build.**

Run: `npm run test`
Run: `npm run build`
Expected: PASS.

- [ ] **Step 5: Commit.**

Commit: `feat(admin): add quiz management UI`

### Task 10: Integrar documentação, Compose e validação final

**Files:**
- Modify: `.env.example`
- Modify: `README.md`
- Modify: `compose.yml`, `compose.dev.yml`, `compose.prod.yml`
- Modify: `docs/superpowers/specs/2026-08-19-quiz-mvp-design.md` apenas se a implementação revelar uma discrepância objetiva

**Interfaces:**
- Produces instruções locais sem segredos para configurar o administrador inicial e executar testes.

- [ ] **Step 1: Adicionar apenas placeholders fictícios de bootstrap ao exemplo e documentar que `.env` é local e que a password inicial não é registada.**

- [ ] **Step 2: Validar a configuração sem interpolar segredos.**

Run: `docker compose -f compose.yml -f compose.dev.yml config --no-interpolate --quiet`
Run: `docker compose -f compose.yml -f compose.prod.yml config --no-interpolate --quiet`
Expected: ambos exit 0.

- [ ] **Step 3: Executar a suite completa e o stack real.**

Run: `dotnet test GameSphere_backend.Tests/GameSphere_backend.Tests.csproj`
Run: `npm run test --prefix GameSphere_frontend`
Run: `docker compose -f compose.yml -f compose.dev.yml up --build --detach`
Expected: DB saudável, `migrate` exit 0, API e frontend em execução; smoke tests autenticados de catálogo e administração devolvem os códigos esperados.

- [ ] **Step 4: Rever `git diff --check`, `git status`, resultados de testes e criar o commit final apenas para documentação/Compose se ainda houver alterações próprias.**

Commit: `docs(quiz): document local admin bootstrap`

## Revisão do plano

- Cobertura da especificação: Tasks 2–3 cobrem roles e bootstrap; Tasks 4–5 catálogo, proteção de respostas e tentativas; Task 6 CRUD administrativo; Tasks 7–9 frontend; Tasks 1 e 7–10 testes e validação.
- Segurança: roles persistentes, JWT com claim de role, input validado no servidor, nenhuma resposta correta para participantes, valores iniciais só em ambiente e PostgreSQL efémero para testes.
- Consistência: todas as rotas e métodos definidos nas tarefas usam os contratos de DTO declarados; a submissão depende do serviço criado na Task 4; a UI depende das APIs das Tasks 4–6.
- Fora de âmbito preservado: não há tarefas para ranking, XP, estatísticas, gestão de utilizadores, uploads ou limites de tentativas.
