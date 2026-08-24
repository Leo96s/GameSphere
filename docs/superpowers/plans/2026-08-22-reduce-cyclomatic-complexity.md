# Reduce Cyclomatic Complexity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reduzir todos os métodos/funções de produção acima de 10 para um máximo de 10, baixar as agregações das classes que concentram responsabilidades e manter o comportamento HTTP, de autenticação, de persistência e de apresentação.

**Architecture:** Aplicar decomposição por caso de uso na camada de serviços do ASP.NET Core. As controllers ficam finas e agrupadas por fluxo HTTP, mantendo a rota pública `api/User`; a validação e a pontuação de quizzes passam para componentes coesos e testáveis. As suites de integração são divididas por comportamento para que a complexidade agregada das classes de teste deixe de esconder cenários diferentes.

**Tech Stack:** ASP.NET Core/.NET 10, C#, EF Core/Npgsql, xUnit v3/Testcontainers PostgreSQL, Vue 3/Vite, Vitest e Playwright.

## Global Constraints

- Não adicionar dependências NuGet ou npm; reutilizar `ServiceResponse`, EF Core, Testcontainers, xUnit, Vitest e os serviços existentes.
- Não alterar rotas, verbos HTTP, nomes dos campos JSON, políticas de autorização, cookies HttpOnly, CORS, rate limiting ou regras de pontuação.
- Não criar migrações nem alterar o esquema da base de dados; a mudança é exclusivamente de organização e orquestração.
- Validar toda a entrada no servidor; validação no frontend continua apenas como melhoria de experiência.
- Não colocar segredos, tokens, passwords, connection strings ou dados pessoais reais em código, testes, documentação ou commits.
- Antes de cada refactor, acrescentar ou confirmar testes de caracterização; depois de cada tarefa correr o conjunto de testes dessa área.
- Não alterar o frontend de produção: o maior método/função frontend confirmado está em 7, abaixo do limiar.
- Não commitar automaticamente; cada commit abaixo é uma proposta que só deve ser criado após revisão e verificação.

## Baseline confirmado

O relatório estático actual confirmou estes pontos prioritários:

- `GameSphere_backend/Services/UserServices.cs`: `EditUserAsync` = 19, `CreateNewUserAsync` = 11, `SocialLoginAsync` = 11, classe = 97.
- `GameSphere_backend/Services/QuizAdministrationService.cs`: `TryNormalizeQuestion` = 13, classe = 72.
- `GameSphere_backend/Services/QuizCatalogService.cs`: `SubmitAttemptAsync` = 12, classe = 17.
- `GameSphere_backend/Controllers/UserController.cs`: classe = 27.
- Controllers administrativas: `AdminQuizzesController` = 18 e `AdminQuestionsController` = 15.
- `InitialAdminBootstrapper` = 11.
- Suites de teste: `UserAccountTests` = 29, `QuizAttemptTests` = 24, `QuizAdministrationTests` = 19, `QuizCatalogTests` = 11 e `InitialAdminBootstrapperTests` = 11.

Política de medição a manter: `1 + if/ciclo/catch/arm de switch/ternário/&&/||/or`; `??` e `?.` não são decisões; branches de templates Vue não são contados porque são compilados para render functions.

## Estrutura de ficheiros e responsabilidades

| Ficheiro/módulo | Responsabilidade única |
|---|---|
| `GameSphere_backend/Interfaces/IUserQueryService.cs` | Ler uma conta por ID ou email e devolver DTOs. |
| `GameSphere_backend/Interfaces/IUserRegistrationService.cs` | Validar e criar uma conta local. |
| `GameSphere_backend/Interfaces/IUserProfileService.cs` | Validar e actualizar o perfil de uma conta. |
| `GameSphere_backend/Interfaces/IUserDeletionService.cs` | Eliminar uma conta e os dados relacionados. |
| `GameSphere_backend/Interfaces/IPasswordLoginService.cs` | Autenticar credenciais locais e emitir a sessão. |
| `GameSphere_backend/Interfaces/ISocialLoginService.cs` | Resolver uma identidade Firebase verificada e emitir a sessão. |
| `GameSphere_backend/Interfaces/IPasswordRecoveryService.cs` | Expor os três passos públicos da recuperação de password. |
| `GameSphere_backend/Services/UserQueryService.cs` | Implementar leituras de utilizadores sem mutações. |
| `GameSphere_backend/Services/UserRegistrationService.cs` | Implementar criação de utilizadores sem autenticação social. |
| `GameSphere_backend/Services/UserProfileService.cs` | Implementar edição de perfil e revogação após mudança de password. |
| `GameSphere_backend/Services/UserDeletionService.cs` | Implementar eliminação transaccional da conta. |
| `GameSphere_backend/Services/PasswordLoginService.cs` | Implementar login por email/password. |
| `GameSphere_backend/Services/SocialLoginService.cs` | Implementar login com `FirebaseUserInfo` já verificado. |
| `GameSphere_backend/Services/PasswordRecoveryService.cs` | Orquestrar pedido, validação e aplicação do reset. |
| `GameSphere_backend/Services/PasswordResetInputValidator.cs` | Validar email, código e password sem I/O. |
| `GameSphere_backend/Services/PasswordResetCodeService.cs` | Criar, verificar, contar tentativas e invalidar códigos. |
| `GameSphere_backend/Services/AuthenticationCookieService.cs` | Escrever e apagar o cookie de sessão com as opções actuais. |
| `GameSphere_backend/Controllers/UserProfileController.cs` | Servir leitura, actualização e eliminação por ID. |
| `GameSphere_backend/Controllers/UserLookupController.cs` | Servir a leitura autenticada por email. |
| `GameSphere_backend/Controllers/UserRegistrationController.cs` | Servir o POST de criação de conta. |
| `GameSphere_backend/Controllers/UserAuthenticationController.cs` | Servir login, logout e recuperação de password. |
| `GameSphere_backend/Services/QuizRequestNormalizer.cs` | Normalizar título, dificuldade e lista de perguntas de um quiz. |
| `GameSphere_backend/Services/QuestionRequestNormalizer.cs` | Normalizar uma pergunta através de validadores coesos. |
| `GameSphere_backend/Services/QuestionShapeValidator.cs` | Validar campos obrigatórios e limites estruturais da pergunta. |
| `GameSphere_backend/Services/QuestionAnswersValidator.cs` | Normalizar opções e rejeitar opções vazias ou repetidas. |
| `GameSphere_backend/Services/CorrectAnswerValidator.cs` | Confirmar que a resposta correcta pertence às opções. |
| `GameSphere_backend/Services/QuizAdministrationService.cs` | Manter a interface pública e delegar cada operação administrativa. |
| `GameSphere_backend/Services/QuizAttemptValidator.cs` | Validar uma tentativa contra as perguntas publicadas do quiz. |
| `GameSphere_backend/Services/QuizAttemptScorer.cs` | Calcular respostas certas sem persistência nem acesso HTTP. |
| `GameSphere_backend/Services/QuizAttemptPersistenceService.cs` | Persistir o `Score` depois de a tentativa estar validada. |
| `GameSphere_backend.Tests/Infrastructure/*` | Reutilizar fixture PostgreSQL, fábrica HTTP e dados fictícios partilhados. |

O padrão arquitectural é uma decomposição por caso de uso dentro da convenção MVC/ASP.NET Core: controllers orquestram HTTP, services coordenam casos de uso e validadores/scorers são componentes puros. Esta divisão é justificada porque os avisos surgem precisamente de classes de serviço que misturam validação, persistência, autenticação e apresentação.

## Task 1: Fixar a baseline e escrever testes de caracterização

**Files:**
- Modify: `GameSphere_backend.Tests/Authentication/UserAccountTests.cs`
- Modify: `GameSphere_backend.Tests/Authentication/InitialAdminBootstrapperTests.cs`
- Modify: `GameSphere_backend.Tests/Quizzes/QuizAttemptTests.cs`
- Modify: `GameSphere_backend.Tests/Admin/QuizAdministrationTests.cs`
- Modify: `GameSphere_backend.Tests/Quizzes/QuizCatalogTests.cs`
- Create: `GameSphere_backend.Tests/Services/PasswordRecoveryServiceTests.cs`
- Create: `GameSphere_backend.Tests/Services/QuestionRequestNormalizerTests.cs`
- Create: `GameSphere_backend.Tests/Services/QuizAttemptValidatorTests.cs`

**Interfaces:**
- Consumes: os contratos HTTP e `ServiceResponse` actuais.
- Produces: cobertura explícita para os ramos que serão extraídos nas tarefas seguintes.

- [ ] **Step 1: Confirmar os testes existentes antes de mover código**

Run:

```powershell
dotnet test GameSphere_backend.Tests/GameSphere_backend.Tests.csproj --configuration Release
npm run test --prefix GameSphere_frontend
```

Expected: todos os testes actuais passam; se falharem, corrigir a baseline antes de refactorizar.

- [ ] **Step 2: Acrescentar os casos de caracterização que faltam**

Cobrir explicitamente, usando apenas dados fictícios:

Criar estes testes com os helpers e padrões já existentes nas suites:

```text
Profile_update_rejects_invalid_profile_without_persisting_changes
Social_login_rejects_conflicting_verified_identity
Password_recovery_rejects_malformed_code_before_database_lookup
Question_normalizer_rejects_duplicate_options
Attempt_validator_rejects_unknown_question_ids
```

Cada teste deve verificar o status/resposta indicado pelo nome e, quando aplicável, que a entidade não foi alterada. Usar apenas dados fictícios e não introduzir credenciais reais.

- [ ] **Step 3: Verificar novamente a baseline**

Run os dois comandos do Step 1. Expected: PASS e o mesmo comportamento HTTP antes da extracção.

- [ ] **Step 4: Commit proposto após aprovação**

```powershell
git add GameSphere_backend.Tests
git commit -m "test(complexity): add characterization coverage"
```

## Task 2: Separar os fluxos de conta e autenticação

**Files:**
- Create: `GameSphere_backend/Interfaces/IUserQueryService.cs`
- Create: `GameSphere_backend/Interfaces/IUserRegistrationService.cs`
- Create: `GameSphere_backend/Interfaces/IUserProfileService.cs`
- Create: `GameSphere_backend/Interfaces/IUserDeletionService.cs`
- Create: `GameSphere_backend/Interfaces/IPasswordLoginService.cs`
- Create: `GameSphere_backend/Interfaces/ISocialLoginService.cs`
- Create: `GameSphere_backend/Interfaces/IPasswordRecoveryService.cs`
- Create: `GameSphere_backend/Services/UserQueryService.cs`
- Create: `GameSphere_backend/Services/UserRegistrationService.cs`
- Create: `GameSphere_backend/Services/UserProfileService.cs`
- Create: `GameSphere_backend/Services/UserDeletionService.cs`
- Create: `GameSphere_backend/Services/PasswordLoginService.cs`
- Create: `GameSphere_backend/Services/SocialLoginService.cs`
- Create: `GameSphere_backend/Services/PasswordRecoveryService.cs`
- Create: `GameSphere_backend/Services/AuthenticationCookieService.cs`
- Delete: `GameSphere_backend/Interfaces/IUserService.cs`
- Delete: `GameSphere_backend/Services/UserServices.cs`
- Modify: `GameSphere_backend/Program.cs:67`
- Modify: `GameSphere_backend/Controllers/UserController.cs`
- Create: `GameSphere_backend/Controllers/UserProfileController.cs`
- Create: `GameSphere_backend/Controllers/UserLookupController.cs`
- Create: `GameSphere_backend/Controllers/UserRegistrationController.cs`
- Create: `GameSphere_backend/Controllers/UserAuthenticationController.cs`
- Modify: `GameSphere_backend.Tests/Authentication/InitialAdminBootstrapperTests.cs`

**Interfaces:**

```csharp
public interface IUserQueryService
{
    Task<ServiceResponse<UserDto>> GetUserByIdAsync(int id);
    Task<ServiceResponse<UserDto>> GetUserByEmailAsync(string email);
}

public interface IUserRegistrationService
{
    Task<ServiceResponse<UserDto>> CreateNewUserAsync(RegisterUserRequest user);
}

public interface IUserProfileService
{
    Task<ServiceResponse<UserDto>> EditUserAsync(int id, UpdateUserRequest updatedUser);
}

public interface IUserDeletionService
{
    Task<ServiceResponse<string>> DeleteUserAsync(int id);
}

public interface IPasswordLoginService
{
    Task<ServiceResponse<LoginResponse>> LoginAsync(LoginRequest request);
}

public interface ISocialLoginService
{
    Task<ServiceResponse<LoginResponse>> SocialLoginAsync(FirebaseUserInfo firebaseUser);
}

public interface IPasswordRecoveryService
{
    Task<ServiceResponse<bool>> SendPasswordResetCode(string email);
    Task<ServiceResponse<bool>> ValidateResetCode(string email, string resetCode);
    Task<ServiceResponse<bool>> ResetPassword(string email, string resetCode, string newPassword);
}
```

- [ ] **Step 1: Criar os contratos e registar as implementações no DI**

Substituir:

```csharp
builder.Services.AddScoped<IUserService, UserServices>();
```

por registos `Scoped` um-para-um para os sete contratos novos. Não alterar as configurações de autenticação, rate limiting ou cookies.

- [ ] **Step 2: Extrair as operações sem mudar as mensagens**

Mover cada método de `UserServices` para o serviço correspondente, mantendo os tipos `ServiceResponse`, `Type`, mensagens, `AuthVersion`, `EmailNormalizer`, `BCrypt`, logger e mapeadores. Os serviços devem receber apenas as dependências usadas pelo caso de uso.

Metas de complexidade:

- `UserQueryService`: cada método até 4; agregação até 10.
- `UserRegistrationService`: `CreateNewUserAsync` até 8.
- `UserProfileService`: `EditUserAsync` até 9.
- `UserDeletionService`: `DeleteUserAsync` até 4.
- `PasswordLoginService`: `LoginAsync` até 9, extraindo validação preliminar.
- `SocialLoginService`: `SocialLoginAsync` até 9, extraindo resolução de identidade/conflicto.
- `PasswordRecoveryService`: três métodos de delegação simples; a lógica de código fica em `PasswordResetCodeService`.

- [ ] **Step 3: Separar os endpoints sem alterar URLs**

Todos os novos controllers devem declarar explicitamente `[Route("api/User")]`. Manter estes endpoints:

```text
GET    /api/User/by-id/{id}
GET    /api/User/by-email/{email}
POST   /api/User
PUT    /api/User/{id}
DELETE /api/User/{id}
POST   /api/User/login
POST   /api/User/social-login
POST   /api/User/send-reset-code
POST   /api/User/validate-reset-code
POST   /api/User/reset-password
POST   /api/User/logout
```

`UserProfileController` mantém a autorização self-service; `UserAuthenticationController` mantém `EnableRateLimiting("auth")`; `AuthenticationCookieService` conserva `HttpOnly`, `Secure`, `SameSite=Lax`, `Path=/` e a expiração configurada.

- [ ] **Step 4: Actualizar testes de compilação e isolamento**

Actualizar apenas referências de tipos nos testes, preservando os testes HTTP já existentes. Confirmar que utilizadores não conseguem consultar, actualizar ou apagar contas alheias.

- [ ] **Step 5: Verificar e medir**

Run:

```powershell
dotnet test GameSphere_backend.Tests/GameSphere_backend.Tests.csproj --configuration Release
```

Expected: PASS; depois reexecutar a análise de complexidade e confirmar que `EditUserAsync`, `CreateNewUserAsync`, `SocialLoginAsync` e `UserController` deixaram de aparecer acima de 10.

- [ ] **Step 6: Commit proposto após aprovação**

```powershell
git add GameSphere_backend/Interfaces GameSphere_backend/Services GameSphere_backend/Controllers GameSphere_backend/Program.cs GameSphere_backend.Tests/Authentication/InitialAdminBootstrapperTests.cs
git commit -m "refactor(account): split user workflows by responsibility"
```

## Task 3: Isolar a recuperação de password

**Files:**
- Create: `GameSphere_backend/Services/PasswordResetInputValidator.cs`
- Create: `GameSphere_backend/Services/PasswordResetCodeService.cs`
- Modify: `GameSphere_backend/Services/PasswordRecoveryService.cs`
- Test: `GameSphere_backend.Tests/Services/PasswordRecoveryServiceTests.cs`
- Test: `GameSphere_backend.Tests/Authentication/UserAccountTests.cs`

**Interfaces:**
- Consumes: `IPasswordRecoveryService`, `AppDbContext`, `IEmailService`, `EmailNormalizer` e `ServiceResponse`.
- Produces: `PasswordRecoveryService` com três métodos de orquestração simples; `PasswordResetInputValidator` sem I/O; `PasswordResetCodeService` responsável por hash, expiração, tentativas e limpeza.

- [ ] **Step 1: Cobrir a fronteira do validador**

Testar email inválido, código não numérico, código com tamanho incorrecto, password curta, password longa, código expirado e quinta tentativa inválida. Não testar valores de produção nem guardar códigos reais em ficheiros.

- [ ] **Step 2: Extrair validação sem base de dados**

Usar uma assinatura explícita e determinística:

```csharp
public interface IPasswordResetInputValidator
{
    string? ValidateRequest(string email, string resetCode, string? newPassword = null);
}
```

A implementação deve manter a mensagem genérica actual para não revelar se uma conta existe.

- [ ] **Step 3: Extrair a gestão do código**

Mover para `PasswordResetCodeService` a verificação BCrypt, a contagem de tentativas, a expiração e a limpeza após cinco falhas. A classe não deve enviar emails nem construir respostas HTTP.

- [ ] **Step 4: Reduzir os métodos públicos ao fluxo de orquestração**

`SendPasswordResetCode`, `ValidateResetCode` e `ResetPassword` devem apenas validar entrada, procurar o utilizador, chamar o serviço de código, persistir e devolver a resposta actual. A condição de validade do código não deve ser duplicada entre os três fluxos.

- [ ] **Step 5: Verificar**

```powershell
dotnet test GameSphere_backend.Tests/GameSphere_backend.Tests.csproj --configuration Release
```

Expected: PASS; `ResetPassword` e `ValidateResetCode` ficam abaixo de 10 e o comportamento de expiração, rate limiting e revogação de sessão permanece coberto.

- [ ] **Step 6: Commit proposto após aprovação**

```powershell
git add GameSphere_backend/Services GameSphere_backend.Tests/Services GameSphere_backend.Tests/Authentication/UserAccountTests.cs
git commit -m "refactor(auth): isolate password recovery rules"
```

## Task 4: Separar normalização e operações administrativas de quizzes

**Files:**
- Create: `GameSphere_backend/Services/QuizRequestNormalizer.cs`
- Create: `GameSphere_backend/Services/QuestionRequestNormalizer.cs`
- Create: `GameSphere_backend/Services/QuestionShapeValidator.cs`
- Create: `GameSphere_backend/Services/QuestionAnswersValidator.cs`
- Create: `GameSphere_backend/Services/CorrectAnswerValidator.cs`
- Create: `GameSphere_backend/Services/QuizAdministration/CreateQuizHandler.cs`
- Create: `GameSphere_backend/Services/QuizAdministration/UpdateQuizHandler.cs`
- Create: `GameSphere_backend/Services/QuizAdministration/DeleteQuizHandler.cs`
- Create: `GameSphere_backend/Services/QuizAdministration/CreateQuestionHandler.cs`
- Create: `GameSphere_backend/Services/QuizAdministration/UpdateQuestionHandler.cs`
- Create: `GameSphere_backend/Services/QuizAdministration/DeleteQuestionHandler.cs`
- Modify: `GameSphere_backend/Services/QuizAdministrationService.cs`
- Modify: `GameSphere_backend.Tests/Admin/QuizAdministrationTests.cs`
- Create: `GameSphere_backend.Tests/Services/QuestionRequestNormalizerTests.cs`

**Interfaces:**
- Consumes: `IQuizAdministrationService` e os DTOs `AdminQuizUpsertDto`/`AdminQuestionUpsertDto`.
- Produces: uma fachada com os oito métodos actuais, cada um delegando para um handler de caso de uso; validadores sem acesso à base de dados.

- [ ] **Step 1: Testar a normalização isoladamente**

Cobrir título vazio/longo, dificuldade inválida, mais de 100 perguntas, pergunta sem descrição, menos de duas opções, opções repetidas, opções vazias, opção demasiado longa e resposta correcta ausente.

- [ ] **Step 2: Separar a validação da pergunta em regras coesas**

Cada regra deve ter uma responsabilidade e um método abaixo de 10:

```csharp
public interface IQuestionShapeValidator
{
    string? Validate(AdminQuestionUpsertDto? request);
}

public interface IQuestionAnswersValidator
{
    string? ValidateAndNormalize(AdminQuestionUpsertDto request, out string[] answers);
}

public interface ICorrectAnswerValidator
{
    string? Validate(string[] answers, string? correctAnswer);
}
```

`QuestionRequestNormalizer` apenas coordena estes três resultados e constrói o DTO normalizado. Não duplicar regras no frontend.

- [ ] **Step 3: Extrair a normalização do quiz**

`QuizRequestNormalizer` mantém a validação do título/dificuldade e percorre as perguntas chamando `QuestionRequestNormalizer`. A normalização deve continuar a devolver exactamente as mensagens e os limites actuais.

- [ ] **Step 4: Transformar `QuizAdministrationService` numa fachada**

Manter a assinatura de `IQuizAdministrationService`. Separar as operações de leitura, criação/actualização/remoção de quizzes e criação/actualização/remoção de perguntas em handlers com uma operação principal cada. Manter transacções, `FOR UPDATE`, verificação do administrador e tratamento de foreign keys nos handlers correspondentes.

- [ ] **Step 5: Verificar autorização, concorrência e transacções**

```powershell
dotnet test GameSphere_backend.Tests/GameSphere_backend.Tests.csproj --configuration Release
```

Expected: PASS para autorização administrativa, quizzes com tentativas, contagens concorrentes de perguntas e rollback em falhas. `TryNormalizeQuestion` desaparece e nenhum handler individual ultrapassa 10.

- [ ] **Step 6: Commit proposto após aprovação**

```powershell
git add GameSphere_backend/Services GameSphere_backend.Tests/Admin GameSphere_backend.Tests/Services
git commit -m "refactor(quizzes): split administration validation and commands"
```

## Task 5: Isolar validação, pontuação e persistência da tentativa

**Files:**
- Create: `GameSphere_backend/Services/QuizAttemptValidator.cs`
- Create: `GameSphere_backend/Services/QuizAttemptScorer.cs`
- Create: `GameSphere_backend/Services/QuizAttemptPersistenceService.cs`
- Modify: `GameSphere_backend/Services/QuizCatalogService.cs`
- Modify: `GameSphere_backend.Tests/Quizzes/QuizAttemptTests.cs`
- Create: `GameSphere_backend.Tests/Services/QuizAttemptValidatorTests.cs`

**Interfaces:**
- Consumes: `Quizz`, `Question`, `QuizAttemptRequest`, `QuizAnswerRequest` e `Score`.
- Produces: `SubmitAttemptAsync` como orquestrador; o validador rejeita tentativas inválidas, o scorer devolve a pontuação e o persister grava o score.

- [ ] **Step 1: Criar um resultado interno de validação**

Usar um tipo interno sem exposição HTTP:

```csharp
public sealed record ValidatedQuizAttempt(
    IReadOnlyList<Question> Questions,
    IReadOnlyDictionary<int, QuizAnswerRequest> AnswersByQuestion);
```

- [ ] **Step 2: Mover todas as rejeições para `QuizAttemptValidator`**

Preservar as mensagens para quiz inexistente, quiz sem perguntas, resposta em falta, duplicada, pergunta desconhecida e opção inválida. O método deve devolver `ServiceResponse<ValidatedQuizAttempt>`.

- [ ] **Step 3: Mover a contagem para `QuizAttemptScorer`**

O scorer recebe apenas a tentativa validada e devolve `int correctAnswers`; não lê claims, não grava dados e não calcula respostas a partir de informação enviada pelo cliente que não tenha sido validada.

- [ ] **Step 4: Mover a criação do `Score` para o persister**

O persister recebe `userId`, `quizId` e a pontuação, define `GameId = null`, `Date = DateTime.UtcNow` e chama `SaveChangesAsync`.

- [ ] **Step 5: Verificar o fluxo completo**

```powershell
dotnet test GameSphere_backend.Tests/GameSphere_backend.Tests.csproj --configuration Release
```

Expected: PASS; `SubmitAttemptAsync` fica abaixo de 10, não expõe `CorrectAnswer` no catálogo e mantém tentativas repetidas e percentagens actuais.

- [ ] **Step 6: Commit proposto após aprovação**

```powershell
git add GameSphere_backend/Services/QuizCatalogService.cs GameSphere_backend/Services/QuizAttemptValidator.cs GameSphere_backend/Services/QuizAttemptScorer.cs GameSphere_backend/Services/QuizAttemptPersistenceService.cs GameSphere_backend.Tests/Quizzes/QuizAttemptTests.cs GameSphere_backend.Tests/Services/QuizAttemptValidatorTests.cs
git commit -m "refactor(quizzes): isolate attempt validation and scoring"
```

## Task 6: Reduzir agregações das controllers e do bootstrapper

**Files:**
- Modify: `GameSphere_backend/Controllers/AdminQuizzesController.cs`
- Modify: `GameSphere_backend/Controllers/AdminQuestionsController.cs`
- Modify: `GameSphere_backend/Services/InitialAdminBootstrapper.cs`
- Create: `GameSphere_backend/Services/InitialAdminConfiguration.cs`
- Create: `GameSphere_backend/Services/InitialAdminProvisioner.cs`
- Modify: `GameSphere_backend.Tests/Authentication/InitialAdminBootstrapperTests.cs`

**Interfaces:**
- Consumes: as policies existentes `ActiveAdminRequirement` e a configuração `InitialAdmin`.
- Produces: controllers administrativas sem duplicação de transformação de erros; bootstrapper com configuração, promoção e criação concorrente separadas.

- [ ] **Step 1: Extrair mapeamento comum dos erros administrativos**

Criar um método de mapeamento partilhado para `NotFound`, `Conflict` e `BadRequest`, sem alterar os status codes. Manter a obtenção do ID a partir de `ClaimTypes.NameIdentifier`/`sub` e não confiar apenas no role do token.

- [ ] **Step 2: Extrair validação de configuração do administrador**

`InitialAdminConfiguration` valida email e password e devolve valores tipados; não escreve logs com os valores. `InitialAdminProvisioner` mantém a criação idempotente, promoção e recuperação do conflito de constraint.

- [ ] **Step 3: Verificar segurança e concorrência**

```powershell
dotnet test GameSphere_backend.Tests/GameSphere_backend.Tests.csproj --configuration Release
```

Expected: PASS para admin inactivo, utilizador normal, arranque idempotente e arranque concorrente; nenhuma configuração sensível aparece no output.

- [ ] **Step 4: Commit proposto após aprovação**

```powershell
git add GameSphere_backend/Controllers/AdminQuizzesController.cs GameSphere_backend/Controllers/AdminQuestionsController.cs GameSphere_backend/Services/InitialAdminBootstrapper.cs GameSphere_backend/Services/InitialAdminConfiguration.cs GameSphere_backend/Services/InitialAdminProvisioner.cs GameSphere_backend.Tests/Authentication/InitialAdminBootstrapperTests.cs
git commit -m "refactor(admin): separate controller and bootstrap responsibilities"
```

## Task 7: Dividir suites de teste por comportamento

**Files:**
- Create: `GameSphere_backend.Tests/Authentication/UserIsolationTests.cs`
- Create: `GameSphere_backend.Tests/Authentication/UserSessionTests.cs`
- Create: `GameSphere_backend.Tests/Authentication/PasswordRecoveryTests.cs`
- Create: `GameSphere_backend.Tests/Admin/AdminAuthorizationTests.cs`
- Create: `GameSphere_backend.Tests/Admin/QuizLifecycleTests.cs`
- Create: `GameSphere_backend.Tests/Admin/QuestionConcurrencyTests.cs`
- Create: `GameSphere_backend.Tests/Quizzes/QuizCatalogAccessTests.cs`
- Create: `GameSphere_backend.Tests/Quizzes/QuizAttemptValidationTests.cs`
- Create: `GameSphere_backend.Tests/Quizzes/QuizAttemptPersistenceTests.cs`
- Delete after migration: `GameSphere_backend.Tests/Authentication/UserAccountTests.cs`
- Delete after migration: `GameSphere_backend.Tests/Admin/QuizAdministrationTests.cs`
- Delete after migration: `GameSphere_backend.Tests/Quizzes/QuizCatalogTests.cs`
- Delete after migration: `GameSphere_backend.Tests/Quizzes/QuizAttemptTests.cs`
- Delete after migration: `GameSphere_backend.Tests/Authentication/InitialAdminBootstrapperTests.cs`
- Reuse: `GameSphere_backend.Tests/Infrastructure/PostgreSqlFixture.cs`
- Reuse: `GameSphere_backend.Tests/Infrastructure/GameSphereApiFactory.cs`

**Interfaces:**
- Consumes: os mesmos fixtures, helpers e dados fictícios actuais.
- Produces: classes de teste agrupadas por um comportamento, sem alterar o número ou o significado dos cenários.

- [ ] **Step 1: Mover testes sem alterar o corpo**

Começar pelos testes de isolamento de utilizadores, depois sessões/recuperação, administração e tentativas. Cada novo ficheiro deve conter apenas um grupo coeso e uma fixture existente.

- [ ] **Step 2: Extrair apenas helpers partilhados comprovadamente repetidos**

Colocar a criação de pedidos autenticados e entidades fictícias em ficheiros de infraestrutura pequenos. Não criar uma classe `Utils` genérica; cada helper deve ter uma única responsabilidade.

- [ ] **Step 3: Executar a suite completa**

```powershell
dotnet test GameSphere_backend.Tests/GameSphere_backend.Tests.csproj --configuration Release
```

Expected: PASS; nenhuma classe de testes agrupada por comportamento fica acima de 10, ou uma eventual excepção é documentada com a agregação exacta.

- [ ] **Step 4: Commit proposto após aprovação**

```powershell
git add GameSphere_backend.Tests
git commit -m "test(complexity): split integration suites by behavior"
```

## Task 8: Verificação final e decisão sobre excepções

**Files:**
- Modify only if needed: `docs/superpowers/plans/2026-08-22-reduce-cyclomatic-complexity.md`
- No production file changes unless a previous task fails its acceptance criteria.

- [ ] **Step 1: Verificar backend**

```powershell
dotnet test GameSphere_backend.Tests/GameSphere_backend.Tests.csproj --configuration Release
```

Expected: todos os testes passam sem warnings de compilação introduzidos.

- [ ] **Step 2: Verificar frontend sem alterações de comportamento**

```powershell
npm run test --prefix GameSphere_frontend
npm run test:coverage --prefix GameSphere_frontend
npm run build --prefix GameSphere_frontend
```

Expected: testes, cobertura configurada e build passam; não são necessárias novas dependências.

- [ ] **Step 3: Verificar configuração Docker e higiene do diff**

```powershell
docker compose -f compose.yml -f compose.dev.yml config --quiet
docker compose -f compose.yml -f compose.prod.yml config --quiet
git diff --check
git status --short
```

Expected: configurações válidas, diff sem whitespace errors e apenas ficheiros previstos alterados.

- [ ] **Step 4: Reexecutar a análise de complexidade no repositório inteiro**

Executar novamente `$agent-development-kit:cyclomatic-complexity` sem alvo adicional.

Acceptance criteria:

1. Nenhum método/função de produção acima de 10.
2. `UserServices.cs` deixou de existir ou deixou de concentrar os fluxos separados.
3. `TryNormalizeQuestion` e `SubmitAttemptAsync` foram substituídos por orquestradores abaixo de 10.
4. As classes de controller/service acima de 10 foram divididas ou têm uma justificação explícita de coesão.
5. As suites de teste acima de 10 foram divididas por comportamento.
6. O relatório final lista qualquer excepção restante com cálculo, impacto, testes e razão para não dividir mais.

- [ ] **Step 5: Commit final proposto após aprovação**

```powershell
git add docs/superpowers/plans/2026-08-22-reduce-cyclomatic-complexity.md
git commit -m "docs(complexity): record refactoring verification"
```

## Segurança e critérios de não-regressão

- A autorização self-service continua a verificar o ID persistido no token contra o ID do recurso; separar controllers não pode transformar um endpoint autenticado num endpoint público.
- A autorização administrativa continua a consultar role e conta activa na base de dados em cada pedido.
- O login social continua a aceitar apenas `FirebaseUserInfo` produzido pelo verificador Firebase; nenhum controller deve confiar directamente no `idToken`.
- Passwords e códigos de recuperação continuam a ser tratados apenas no backend, com hash BCrypt, expiração, limite de tentativas e limpeza após utilização/falhas.
- O cookie de sessão continua HttpOnly e com as opções de segurança actuais; nenhuma password, token ou código deve ser escrito em logs.
- A pontuação continua a ser calculada no servidor e o DTO de jogo continua a excluir `CorrectAnswer`.
- Não introduzir alterações ao esquema, seeds permanentes, dados reais, endpoints externos ou dependências novas.

## Handoff

O plano está preparado para execução incremental. A ordem recomendada é Tasks 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8, com revisão após cada commit. A execução pode ser feita por subagentes isolados por tarefa ou inline nesta sessão; ambas as opções exigem a verificação final completa antes de qualquer commit final.
