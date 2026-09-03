# GAM-14 — Desactivar, recuperar e anonimizar a conta

> **Para agentes:** REQUIRED SUB-SKILL: usar `subagent-driven-development` (recomendado) ou `executing-plans` para implementar tarefa a tarefa. As checkboxes (`- [ ]`) servem para acompanhar o progresso. Não commitar sem aprovação explícita após cada tarefa.

**Fonte:** Jira GAM-14 (história), épico GAM-11 "Gestão de perfil e ciclo de vida da conta". Dependência GAM-7 já concluída.

## Segurança

- Não são incluídos segredos, tokens ou credenciais reais neste plano nem nos testes; o `.env` local não foi lido.
- Todas as entradas externas (email, código de recuperação) são validadas no servidor, reutilizando `EmailNormalizer` e o padrão de validação já usado em `PasswordResetInputValidator`.
- A desactivação exige reautenticação (`AccountReauthenticator`, o mesmo mecanismo do GAM-16), que serve de confirmação explícita — não é necessário um campo booleano adicional de confirmação.
- A recuperação é um fluxo não autenticado (o utilizador desactivado não consegue iniciar sessão), pelo que segue a mesma convenção anti-enumeração do `PasswordRecoveryService`: mensagens genéricas independentemente de a conta existir, estar activa, já ter sido anonimizada ou o prazo ter expirado.
- Não é adicionada nenhuma dependência NuGet ou npm nova. O sweep de anonimização usa apenas `BackgroundService`/`IHostedService`, nativos do ASP.NET Core.
- Após anonimização, nenhum dado pessoal (email, nome, imagem, UID, password) permanece recuperável pela aplicação; `Scores`, `Quizzs`, `Games`, `Achievements` mantêm-se intactos porque as FKs relevantes já são anuláveis (decisão de 19/08/2026) e a anonimização não remove linhas, só substitui campos PII do `User`.
- **Risco assinalado no próprio ticket, não resolvido por este plano:** "a solução deve validar a política final com TOC/jurista antes de produção, caso haja obrigações de retenção aplicáveis". Este plano assume que não há obrigações adicionais de retenção legal e implementa anonimização definitiva ao fim de 30 dias. Esta suposição deve ser validada com o utilizador/negócio antes de produção; não é uma decisão técnica.

## Objectivo

Substituir a eliminação imediata e permanente da conta (endpoint `DELETE /api/User/{id}`, usado hoje pelo botão "Delete account" do `ProfilePage.vue`) por um ciclo de três estados: **desactivação** (acesso bloqueado de imediato), **recuperação** (verificada, disponível 30 dias) e **anonimização automática** (definitiva, ao fim desse prazo), sem quebrar quizzes, jogos, pontuações ou outras referências à conta.

## Não-objectivos

- Não implementar a validação jurídica/TOC da política de retenção (ver risco acima).
- Não alterar o fluxo de eliminação administrativa (não existe hoje; fora de âmbito).
- Não tocar em `BaseCrudController<T>` (`GameSphere_backend/Interfaces/BaseCrudController.cs`) — é código morto (nenhuma classe o implementa), confirmado por pesquisa, mas remover código morto não relacionado não faz parte deste ticket.
- Não alterar `ActiveUserAuthorizationHandler`, `ActiveAdminAuthorizationHandler` nem a lógica de bloqueio de login para contas inactivas em `PasswordLoginService`/`SocialLoginService` — já bloqueiam correctamente contas com `isActive = false` e tokens com `AuthVersion` desactualizado; este plano só passa a **produzir** esse estado de forma legítima.

## Decisão de âmbito confirmada com o utilizador

O botão "Delete account" e o endpoint `DELETE /api/User/{id}` são **substituídos** pelo novo fluxo (não coexistem). `IUserDeletionService`, `UserDeletionService` e o `DeleteEntity` do `UserProfileController` deixam de existir.

## Condições de paragem

A implementação de cada tarefa segue no máximo um ciclo de implementação → verificação → uma ronda de correcção. Parar e devolver a decisão ao utilizador, sem tentar mais uma correcção, sempre que:

- Uma verificação (`dotnet test`, `npm run test`, `npm run build`, `docker compose config`) falhar uma segunda vez depois de já ter sido corrigida uma vez dentro da mesma tarefa.
- For necessária uma alteração de âmbito, segurança, arquitectura ou compatibilidade não prevista neste plano (por exemplo: descobrir que `Score`/`Quizz` têm mais referências ao `User` do que o confirmado na baseline, ou que o `BackgroundService` introduzido colide com algum mecanismo de hospedagem já existente).
- A migração EF Core (Task 2) gerar alterações a outras tabelas além das cinco colunas descritas.
- Surgir qualquer ambiguidade sobre a suposição de ausência de obrigações de retenção legal assinalada em Segurança.

Nestes casos, a tarefa em curso pára, o estado é reportado (o que passou, o que falhou, porquê) e aguarda-se decisão explícita antes de continuar.

## Matriz de schemas — nota

O template desta skill pede uma "matriz confirmada dos schemas dos dois hosts". Não identifiquei, na exploração do repositório, um cenário de dois hosts aplicável a este plano — o GameSphere corre um único backend ASP.NET Core e um único frontend Vue por ambiente (dev/prod), sem dois hosts distintos a reconciliar schemas. Presumo que esta linha do contrato é genérica do plugin (reutilizada noutros tipos de tarefa, ex. cliente/servidor MCP) e não se aplica aqui; a confirmar com o utilizador.

## Arquitectura

Decomposição por caso de uso, seguindo a convenção MVC/ASP.NET Core já usada nos fluxos de conta (`UserProfileService`, `PasswordChangeService`, `EmailChangeService`):

- **Desactivação** — reutiliza `ActiveUserRequirement`/`ActiveUserAuthorizationHandler` (só uma conta activa pode desactivar-se a si própria) e `AccountReauthenticator` (confirmação de identidade), seguindo exactamente o padrão do GAM-16.
- **Recuperação** — dois endpoints públicos em `UserAuthenticationController`, ao lado dos de recuperação de password, com o mesmo padrão de código numérico hash + expiração de 15 minutos + limite de 5 tentativas já usado em `PasswordResetCodeService`/`EmailChangeCodeService`. Uma nova classe `AccountRecoveryCodeService` replica esse padrão sem o duplicar na orquestração.
- **Anonimização** — um `IAccountAnonymizer` puro (sem HTTP, testável directamente) que aplica a regra de negócio, envolvido por um `AccountAnonymizationSweeper : BackgroundService` fino que só trata do agendamento periódico. É a primeira introdução de um `BackgroundService` no projecto; como `AppDbContext` e os serviços de conta são `Scoped` e o `BackgroundService` é `Singleton`, o sweeper cria um `IServiceScope` a cada iteração via `IServiceScopeFactory`, o mesmo padrão standard do ASP.NET Core.

## Tech Stack

ASP.NET Core/.NET 10, C#, EF Core/Npgsql, xUnit v3/Testcontainers PostgreSQL, Vue 3/Vite, Vitest.

## Global Constraints

- Não adicionar dependências NuGet ou npm; reutilizar `ServiceResponse`, EF Core, `BCrypt.Net-Next`, `AccountReauthenticator`, `IEmailService`, `AuthenticationCookieService` e os padrões de teste existentes (`UserAccountTestBase`, `PostgreSqlFixture`, `GameSphereApiFactory`).
- Não alterar rotas, políticas de autorização ou contratos JSON não relacionados com esta funcionalidade.
- A janela de recuperação (30 dias) e a validade do código (15 minutos) são constantes de código, tal como as expirações de código já existentes — não são configuráveis via `appsettings`, para seguir a convenção actual.
- A anonimização é idempotente: correr o sweep sobre uma conta já anonimizada não deve alterar nada nem falhar.
- Não colocar segredos, tokens, passwords ou dados pessoais reais em código, testes, documentação ou commits.
- Antes de remover `UserDeletionService`/`IUserDeletionService`, confirmar que não há outras referências (já confirmado: só `UserProfileController`, `Program.cs` e os dois testes de isolamento listados abaixo).
- Não commitar automaticamente; cada commit é uma proposta sujeita a revisão.

## Baseline confirmado

- `User.isActive` existe desde sempre, mas **nunca é escrito como `false` em código de produção** — só é lido (bloqueio de login, `ActiveUserAuthorizationHandler`) ou escrito como `true` (registo, bootstrap do admin, login social). A mensagem `"Account is deactivated. Please check your email for the activation link."` já existe em `PasswordLoginService.cs:61` e `SocialLoginService.cs:54`, mas está morta — não há hoje nenhum caminho que produza esse estado. Este plano torna-a real.
- Não existe qualquer campo de desactivação/recuperação/anonimização no modelo `User` nem nas migrações.
- Não existe nenhum `BackgroundService`/`IHostedService` no projecto (confirmado por pesquisa).
- O endpoint `DELETE /api/User/{id}` (`UserProfileController.DeleteEntity`) chama `IUserDeletionService.DeleteUserAsync`, que remove a linha `User` da base de dados. É usado por:
  - `GameSphere_frontend/src/services/userServices.js` (`deleteUser`), chamado por `ProfilePage.vue` (`deleteAccount`, botão "Delete account").
  - `GameSphere_backend.Tests/Authentication/UserIsolationTests.cs`: `User_cannot_delete_another_user` (linha 71) e `User_can_delete_own_account` (linha 138).
  - `GameSphere_frontend/src/services/userServices.spec.js` (teste do `deleteUser`).
- `GameSphere_backend/Interfaces/BaseCrudController.cs` declara `DeleteEntity` abstracto, mas nenhuma classe o implementa — código morto, fora de âmbito (ver Não-objectivos).
- `PasswordRecoveryService.cs` confirma o padrão exacto de mensagens genéricas anti-enumeração a replicar (`GenericRecoveryMessage`, retorno `Ok`/`true` mesmo quando a conta não existe).
- `UserAuthenticationController` já expõe `send-reset-code`/`validate-reset-code`/`reset-password` como endpoints públicos com `[EnableRateLimiting("auth")]` — os novos endpoints de recuperação de conta seguem o mesmo controller e atributo.
- O frontend já tem uma página `Login/SentCodePage.vue` + `Login/ResetPassword.vue` e as rotas `/forgetPassword/sentCode` / `/forgetPassword/resetPassword` (`router.js`) para o fluxo de reset de password, com as funções correspondentes em `authService.js` (`sentResetCode`, `validateResetCodeRequest`, `resetPassword`) — o novo fluxo de recuperação de conta segue a mesma divisão de ficheiros.

## Estrutura de ficheiros e responsabilidades

| Ficheiro | Responsabilidade única |
|---|---|
| `GameSphere_backend/Models/BackendModels/User.cs` | Adicionar `DeactivatedAt`, `IsAnonymized`, `RecoveryCodeHash`, `RecoveryCodeAttempts`, `RecoveryCodeExpiration`. |
| `GameSphere_backend/Migrations/*_AddAccountDeactivationRecovery.cs` | Migração EF Core gerada para os novos campos. |
| `GameSphere_backend/Models/FrontendModels/DeactivateAccountRequest.cs` | DTO de reautenticação para desactivar (`CurrentPassword?`, `FirebaseIdToken?`). |
| `GameSphere_backend/Models/FrontendModels/RecoverAccountRequest.cs` | DTO `Email` + `Code` para o segundo passo da recuperação. |
| `GameSphere_backend/Interfaces/IAccountDeactivationService.cs` | Contrato: desactivar a própria conta. |
| `GameSphere_backend/Interfaces/IAccountRecoveryService.cs` | Contrato: pedir código de recuperação e recuperar a conta. |
| `GameSphere_backend/Interfaces/IAccountAnonymizer.cs` | Contrato: anonimizar contas cujo prazo de recuperação expirou. |
| `GameSphere_backend/Services/AccountRecoveryCodeService.cs` | Criar, verificar, contar tentativas e limpar o código de recuperação (mesmo padrão de `PasswordResetCodeService`). |
| `GameSphere_backend/Services/AccountDeactivationService.cs` | Reautenticar, marcar `isActive=false`/`DeactivatedAt`, invalidar tokens (`AuthVersion++`), emitir código e email. |
| `GameSphere_backend/Services/AccountRecoveryService.cs` | Orquestrar pedido de código e recuperação, com mensagens genéricas. |
| `GameSphere_backend/Services/AccountAnonymizer.cs` | Implementação pura de `IAccountAnonymizer`: substitui PII, marca `IsAnonymized`. |
| `GameSphere_backend/Services/AccountAnonymizationSweeper.cs` | `BackgroundService` fino que corre `IAccountAnonymizer` periodicamente num scope próprio. |
| `GameSphere_backend/Controllers/UserProfileController.cs` | Substituir `DeleteEntity` por `DeactivateAccount`; injectar `AuthenticationCookieService` para limpar a sessão. |
| `GameSphere_backend/Controllers/UserAuthenticationController.cs` | Adicionar `request-account-recovery` e `recover-account` junto dos endpoints de recuperação de password. |
| `GameSphere_backend/Program.cs` | Registar os novos serviços e `AddHostedService<AccountAnonymizationSweeper>()`; remover o registo de `IUserDeletionService`. |
| Remover: `GameSphere_backend/Services/UserDeletionService.cs`, `GameSphere_backend/Interfaces/IUserDeletionService.cs` | Substituídos pelo novo fluxo. |
| `GameSphere_backend.Tests/Authentication/AccountDeactivationRecoveryTests.cs` | Testes de integração do novo ciclo completo. |
| `GameSphere_backend.Tests/Services/AccountAnonymizerTests.cs` | Testes directos da regra de anonimização (idempotência, preservação de scores). |
| `GameSphere_backend.Tests/Authentication/UserIsolationTests.cs` | Remover os dois testes de `DELETE`; a isolação de `DeactivateAccount` fica coberta no novo ficheiro. |
| `GameSphere_frontend/src/services/userServices.js` | Substituir `deleteUser` por `deactivateAccount`. |
| `GameSphere_frontend/src/services/authService.js` | Adicionar `requestAccountRecovery`, `recoverAccount`. |
| `GameSphere_frontend/src/services/userServices.spec.js` | Substituir o teste de `deleteUser` por `deactivateAccount`. |
| `GameSphere_frontend/src/views/Profile/ProfilePage.vue` | Substituir "Delete account" por "Deactivate account" com nova mensagem de confirmação. |
| `GameSphere_frontend/src/views/Login/AccountRecoverySentCodePage.vue` | Pedir o código de recuperação por email (mirror de `SentCodePage.vue`). |
| `GameSphere_frontend/src/views/Login/AccountRecoveryPage.vue` | Introduzir o código e recuperar a conta (mirror de `ResetPassword.vue`). |
| `GameSphere_frontend/src/router.js` | Adicionar `/recoverAccount/sentCode` e `/recoverAccount/recover`. |

## Task 1: Confirmar a baseline

**Files:** nenhum alterado.

- [x] **Step 1: Correr as suites actuais**

```powershell
dotnet test GameSphere_backend.Tests/GameSphere_backend.Tests.csproj --configuration Release
npm run test --prefix GameSphere_frontend
```

Expected: PASS. Se falhar, corrigir antes de continuar — não construir sobre uma baseline vermelha.

## Task 2: Modelo de dados e migração

**Files:**
- Modify: `GameSphere_backend/Models/BackendModels/User.cs`
- Create: migração EF Core (`dotnet ef migrations add`)

- [x] **Step 1: Adicionar os campos ao modelo**

```csharp
public DateTime? DeactivatedAt { get; set; }
public bool IsAnonymized { get; set; }
public string? RecoveryCodeHash { get; set; }
public int RecoveryCodeAttempts { get; set; }
public DateTime? RecoveryCodeExpiration { get; set; }
```

Seguir exactamente o estilo dos campos `PendingEmailCode*` já existentes (nullable, sem anotações de validação — são geridos apenas pelo servidor).

- [x] **Step 2: Gerar e rever a migração**

```powershell
dotnet ef migrations add AddAccountDeactivationRecovery --project GameSphere_backend/GameSphere_backend.csproj
```

Expected: a migração só adiciona as cinco colunas novas (todas nullable ou com default `false`/`0`); não deve alterar nenhuma outra tabela.

- [x] **Step 3: Aplicar localmente e verificar**

```powershell
docker compose -f compose.yml -f compose.dev.yml up -d db
dotnet ef database update --project GameSphere_backend/GameSphere_backend.csproj
```

Expected: sucesso, sem erros de esquema.

- [ ] **Step 4: Commit proposto após aprovação**

```powershell
git add GameSphere_backend/Models/BackendModels/User.cs GameSphere_backend/Migrations
git commit -m "feat(account): add deactivation and recovery columns"
```

## Task 3: Serviço e endpoint de desactivação

**Files:**
- Create: `GameSphere_backend/Interfaces/IAccountDeactivationService.cs`
- Create: `GameSphere_backend/Services/AccountDeactivationService.cs`
- Create: `GameSphere_backend/Services/AccountRecoveryCodeService.cs`
- Create: `GameSphere_backend/Models/FrontendModels/DeactivateAccountRequest.cs`
- Modify: `GameSphere_backend/Controllers/UserProfileController.cs`
- Modify: `GameSphere_backend/Program.cs`

**Interfaces:**

```csharp
public interface IAccountDeactivationService
{
    Task<ServiceResponse<bool>> DeactivateAsync(int userId, DeactivateAccountRequest request);
}
```

- [x] **Step 1: `AccountRecoveryCodeService`**

Réplica de `PasswordResetCodeService`, mas sobre `RecoveryCodeHash`/`RecoveryCodeAttempts`/`RecoveryCodeExpiration`: `CreateCode()`, `AssignCode(user, code, utcNow)` (expira em 15 minutos), `ValidateAsync(user, code)` (bloqueia ao fim de 5 tentativas), `ClearCode(user)`.

- [x] **Step 2: `AccountDeactivationService.DeactivateAsync`**

Fluxo: procurar utilizador → `AccountReauthenticator.VerifyAsync` (falha ⇒ `Unauthorized`, mesma mensagem de `PasswordChangeService`) → `isActive = false`, `DeactivatedAt = DateTime.UtcNow`, `AuthVersion++` → gerar e atribuir código de recuperação com `AccountRecoveryCodeService` → `SaveChangesAsync` → enviar email com o código e a data-limite de recuperação (30 dias) → devolver sucesso.

- [x] **Step 3: Endpoint**

`UserProfileController`: remover `IUserDeletionService`, `DeleteEntity` e o `using` associado; injectar `AuthenticationCookieService`; adicionar:

```csharp
[Authorize(Policy = ActiveUserRequirement.PolicyName)]
[HttpPost("{id}/deactivate")]
public async Task<IActionResult> DeactivateAccount(int id, [FromBody] DeactivateAccountRequest request)
{
    if (!IsCurrentUser(id)) return Forbid();

    var serviceResponse = await _accountDeactivationService.DeactivateAsync(id, request);
    if (serviceResponse.Success)
    {
        _cookieService.DeleteAccessCookie(Response);
    }

    return HandleResponse(serviceResponse);
}
```

- [x] **Step 4: DI**

`Program.cs`: remover `AddScoped<IUserDeletionService, UserDeletionService>()`; adicionar `AddScoped<AccountRecoveryCodeService>()` e `AddScoped<IAccountDeactivationService, AccountDeactivationService>()`.

- [x] **Step 5: Remover o fluxo antigo**

Apagar `GameSphere_backend/Services/UserDeletionService.cs` e `GameSphere_backend/Interfaces/IUserDeletionService.cs`.

- [x] **Step 6: Verificar**

```powershell
dotnet test GameSphere_backend.Tests/GameSphere_backend.Tests.csproj --configuration Release
```

Expected: os dois testes de `DELETE` em `UserIsolationTests.cs` deixam de existir/compilar até serem migrados na Task 6; se ainda existirem nesta altura, o build falha — por isso a Task 6 (testes) deve ser feita antes de correr a suite completa, ou os dois testes removidos já nesta tarefa.

- [ ] **Step 7: Commit proposto após aprovação**

```powershell
git add GameSphere_backend/Interfaces/IAccountDeactivationService.cs GameSphere_backend/Services/AccountDeactivationService.cs GameSphere_backend/Services/AccountRecoveryCodeService.cs GameSphere_backend/Models/FrontendModels/DeactivateAccountRequest.cs GameSphere_backend/Controllers/UserProfileController.cs GameSphere_backend/Program.cs
git rm GameSphere_backend/Services/UserDeletionService.cs GameSphere_backend/Interfaces/IUserDeletionService.cs
git commit -m "feat(account): replace immediate deletion with self-service deactivation"
```

## Task 4: Recuperação da conta

**Files:**
- Create: `GameSphere_backend/Interfaces/IAccountRecoveryService.cs`
- Create: `GameSphere_backend/Services/AccountRecoveryService.cs`
- Create: `GameSphere_backend/Models/FrontendModels/RecoverAccountRequest.cs`
- Modify: `GameSphere_backend/Controllers/UserAuthenticationController.cs`
- Modify: `GameSphere_backend/Program.cs`

**Interfaces:**

```csharp
public interface IAccountRecoveryService
{
    Task<ServiceResponse<bool>> RequestRecoveryCode(string email);
    Task<ServiceResponse<bool>> RecoverAccount(string email, string code);
}
```

- [x] **Step 1: `RequestRecoveryCode`**

Normalizar email → procurar utilizador. Devolver a **mesma mensagem genérica de sucesso** (`Ok`/`true`) se: a conta não existe, está activa, está anonimizada, ou `DeactivatedAt` já passou os 30 dias — nunca revelar qual destes casos ocorreu. Só quando a conta está desactivada, não anonimizada e dentro dos 30 dias é que se gera e envia o código via `AccountRecoveryCodeService`.

- [x] **Step 2: `RecoverAccount`**

Mesmas condições de elegibilidade do Step 1; se elegível e `AccountRecoveryCodeService.ValidateAsync` confirma o código: `isActive = true`, `DeactivatedAt = null`, `AuthVersion++`, limpar o código de recuperação, `SaveChangesAsync`, enviar email de confirmação. Caso contrário, devolver a mesma mensagem genérica de falha usada em `PasswordRecoveryService` (`InvalidResetCodeMessage`, adaptada). Não emitir cookie de sessão — o utilizador volta a autenticar-se pelo login normal, tal como acontece hoje após `reset-password`.

- [x] **Step 3: Endpoints**

Em `UserAuthenticationController`, junto dos de recuperação de password:

```csharp
[HttpPost("request-account-recovery")]
[EnableRateLimiting("auth")]
public async Task<IActionResult> RequestAccountRecovery([FromBody] string email) =>
    HandleResponse(await _accountRecoveryService.RequestRecoveryCode(email));

[HttpPost("recover-account")]
[EnableRateLimiting("auth")]
public async Task<IActionResult> RecoverAccount([FromBody] RecoverAccountRequest request) =>
    HandleResponse(await _accountRecoveryService.RecoverAccount(request.Email, request.Code));
```

- [x] **Step 4: DI**

`Program.cs`: `AddScoped<IAccountRecoveryService, AccountRecoveryService>()`.

- [x] **Step 5: Testes de caracterização**

Criar `GameSphere_backend.Tests/Authentication/AccountDeactivationRecoveryTests.cs` (base `UserAccountTestBase`, `IClassFixture<PostgreSqlFixture>`) cobrindo:

```text
DeactivateAccount_with_correct_credentials_blocks_login_and_protected_access
DeactivateAccount_with_wrong_credentials_is_rejected
DeactivateAccount_cannot_target_another_users_account
RecoverAccount_with_valid_code_within_window_reactivates_the_account
RecoverAccount_with_wrong_code_does_not_reactivate
RecoverAccount_for_an_account_that_was_never_deactivated_returns_generic_failure
RequestRecoveryCode_for_an_unknown_email_returns_the_generic_success_message
```

- [x] **Step 6: Verificar**

```powershell
dotnet test GameSphere_backend.Tests/GameSphere_backend.Tests.csproj --configuration Release
```

Expected: PASS; uma conta desactivada não consegue autenticar-se (`PasswordLoginService`/`SocialLoginService` já bloqueiam) nem aceder a endpoints protegidos (`ActiveUserAuthorizationHandler` já bloqueia); a recuperação dentro do prazo repõe o acesso após novo login.

- [ ] **Step 7: Commit proposto após aprovação**

```powershell
git add GameSphere_backend/Interfaces/IAccountRecoveryService.cs GameSphere_backend/Services/AccountRecoveryService.cs GameSphere_backend/Models/FrontendModels/RecoverAccountRequest.cs GameSphere_backend/Controllers/UserAuthenticationController.cs GameSphere_backend/Program.cs GameSphere_backend.Tests/Authentication/AccountDeactivationRecoveryTests.cs
git commit -m "feat(account): add verified account recovery within the 30-day window"
```

## Task 5: Anonimização automática

**Files:**
- Create: `GameSphere_backend/Interfaces/IAccountAnonymizer.cs`
- Create: `GameSphere_backend/Services/AccountAnonymizer.cs`
- Create: `GameSphere_backend/Services/AccountAnonymizationSweeper.cs`
- Create: `GameSphere_backend.Tests/Services/AccountAnonymizerTests.cs`
- Modify: `GameSphere_backend/Program.cs`

**Interfaces:**

```csharp
public interface IAccountAnonymizer
{
    Task<int> AnonymizeExpiredAccountsAsync(DateTime utcNow, CancellationToken cancellationToken);
}
```

- [x] **Step 1: `AccountAnonymizer`**

Query: `Users.Where(u => !u.isActive && !u.IsAnonymized && u.DeactivatedAt != null && u.DeactivatedAt <= utcNow.AddDays(-30))`. Para cada conta: `Email = $"deleted-user-{u.Id}@anonymized.gamesphere.invalid"`, `FirstName = "Deleted"`, `LastName = null`, `Image = null`, `UID = null`, `HashedPassword` = novo hash aleatório inutilizável, `HasLocalPassword = false`, limpar todos os campos de código (`ResetCodeHash`, `PendingEmailCodeHash`, `RecoveryCodeHash`, etc.), `IsAnonymized = true`. **Não** tocar em `Id`, `Scores`, `Quizzs`, `Games`, `Achievements`, `Level`, `TotalPoints`, `RegistrationDate`, `Role`. Devolve a contagem de contas processadas.

- [x] **Step 2: `AccountAnonymizationSweeper`**

```csharp
public sealed class AccountAnonymizationSweeper : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AccountAnonymizationSweeper> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SweepInterval);
        do
        {
            using var scope = _scopeFactory.CreateScope();
            var anonymizer = scope.ServiceProvider.GetRequiredService<IAccountAnonymizer>();
            var count = await anonymizer.AnonymizeExpiredAccountsAsync(DateTime.UtcNow, stoppingToken);
            if (count > 0) _logger.LogInformation("Anonymized {Count} expired accounts.", count);
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
```

- [x] **Step 3: DI**

`Program.cs`: `AddScoped<IAccountAnonymizer, AccountAnonymizer>()`; `AddHostedService<AccountAnonymizationSweeper>()`.

- [x] **Step 4: Testes directos (sem esperar pelo timer)**

`AccountAnonymizerTests.cs` chama `IAccountAnonymizer.AnonymizeExpiredAccountsAsync` directamente com um `utcNow` controlado, cobrindo:

```text
AnonymizeExpiredAccountsAsync_anonymizes_an_account_past_the_30_day_window
AnonymizeExpiredAccountsAsync_does_not_touch_an_account_still_within_the_window
AnonymizeExpiredAccountsAsync_is_idempotent_for_an_already_anonymized_account
AnonymizeExpiredAccountsAsync_preserves_existing_scores_and_quizzes
```

- [x] **Step 5: Verificar**

```powershell
dotnet test GameSphere_backend.Tests/GameSphere_backend.Tests.csproj --configuration Release
```

Expected: PASS; nenhuma referência a `Score`/`Quizz`/`Game`/`Achievements` fica órfã ou nula após anonimização.

- [ ] **Step 6: Commit proposto após aprovação**

```powershell
git add GameSphere_backend/Interfaces/IAccountAnonymizer.cs GameSphere_backend/Services/AccountAnonymizer.cs GameSphere_backend/Services/AccountAnonymizationSweeper.cs GameSphere_backend/Program.cs GameSphere_backend.Tests/Services/AccountAnonymizerTests.cs
git commit -m "feat(account): anonymize accounts after the 30-day recovery window"
```

## Task 6: Migrar os testes de isolamento existentes

**Files:**
- Modify: `GameSphere_backend.Tests/Authentication/UserIsolationTests.cs`

- [x] **Step 1: Remover os dois testes de `DELETE`**

Remover `User_cannot_delete_another_user` e `User_can_delete_own_account` (linhas 71 e 138) — o equivalente de isolamento para desactivação já está coberto por `DeactivateAccount_cannot_target_another_users_account` (Task 4).

- [x] **Step 2: Verificar**

```powershell
dotnet test GameSphere_backend.Tests/GameSphere_backend.Tests.csproj --configuration Release
```

- [ ] **Step 3: Commit proposto após aprovação**

```powershell
git add GameSphere_backend.Tests/Authentication/UserIsolationTests.cs
git commit -m "test(account): remove obsolete account deletion isolation tests"
```

## Task 7: Frontend

**Files:**
- Modify: `GameSphere_frontend/src/services/userServices.js`
- Modify: `GameSphere_frontend/src/services/userServices.spec.js`
- Modify: `GameSphere_frontend/src/services/authService.js`
- Modify: `GameSphere_frontend/src/views/Profile/ProfilePage.vue`
- Create: `GameSphere_frontend/src/views/Login/AccountRecoverySentCodePage.vue`
- Create: `GameSphere_frontend/src/views/Login/AccountRecoveryPage.vue`
- Modify: `GameSphere_frontend/src/router.js`

- [x] **Step 1: Serviços**

`userServices.js`: substituir `deleteUser` por:

```js
export const deactivateAccount = async (userId, payload) => {
  const response = await api.post(`/User/${userId}/deactivate`, payload);
  return response.data;
};
```

`authService.js`: adicionar, ao lado de `sentResetCode`/`resetPassword`:

```js
export const requestAccountRecovery = async (email) => {
  const response = await api.post('/User/request-account-recovery', email, {
    headers: { 'Content-Type': 'application/json' },
  });
  return response.data;
};

export const recoverAccount = async (email, code) => {
  const response = await api.post('/User/recover-account', { email, code });
  return response.data;
};
```

- [x] **Step 2: `ProfilePage.vue`**

Substituir `deleteAccount`/o botão "Delete account" por `deactivateAccount`/"Deactivate account", com a confirmação (`window.confirm`) actualizada para referir os 30 dias de recuperação e pedir a password actual antes de chamar o serviço (reutilizando o padrão já existente em `submitPasswordChange` para capturar a password).

- [x] **Step 3: Páginas de recuperação**

Criar `AccountRecoverySentCodePage.vue`/`AccountRecoveryPage.vue` como réplicas de `SentCodePage.vue`/`ResetPassword.vue`, chamando `requestAccountRecovery`/`recoverAccount`; adicionar as rotas `/recoverAccount/sentCode` e `/recoverAccount/recover` em `router.js`, junto das rotas `/forgetPassword/*`.

- [x] **Step 4: Testes**

Actualizar `userServices.spec.js` (substituir o teste de `deleteUser` por `deactivateAccount`); adicionar testes equivalentes para `requestAccountRecovery`/`recoverAccount` em `authService.spec.js`, seguindo o padrão dos testes de `sentResetCode`/`resetPassword` já existentes.

- [x] **Step 5: Verificar**

```powershell
npm run test --prefix GameSphere_frontend
npm run test:coverage --prefix GameSphere_frontend
npm run build --prefix GameSphere_frontend
```

- [ ] **Step 6: Commit proposto após aprovação**

```powershell
git add GameSphere_frontend/src
git commit -m "feat(account): add self-service deactivation and recovery UI"
```

## Task 8: Verificação final

- [x] **Step 1: Suite completa**

```powershell
dotnet test GameSphere_backend.Tests/GameSphere_backend.Tests.csproj --configuration Release
npm run test --prefix GameSphere_frontend
npm run build --prefix GameSphere_frontend
```

- [x] **Step 2: Configuração Docker e higiene do diff**

```powershell
docker compose -f compose.yml -f compose.dev.yml config --quiet
docker compose -f compose.yml -f compose.prod.yml config --quiet
git diff --check
git status --short
```

Acceptance criteria (mapeados aos critérios de aceitação do GAM-14):

1. Desactivar exige reautenticação (confirmação explícita) — coberto por `AccountDeactivationRecoveryTests`.
2. Conta desactivada não inicia sessão nem acede a recursos protegidos — coberto pelos handlers já existentes, verificado nos novos testes.
3. Recuperação verificada dentro de 30 dias repõe o acesso — coberto por `RecoverAccount_with_valid_code_within_window_reactivates_the_account`.
4. Anonimização idempotente, documentada, preserva `Scores`/`Quizzs`/`Games`/`Achievements` — coberto por `AccountAnonymizerTests`.
5. Após anonimização, os dados pessoais deixam de ser recuperáveis — confirmado pelo scan de `AccountAnonymizer` (nenhum campo PII original persiste).
6. Testes para desactivação, bloqueio, recuperação dentro do prazo, expiração e anonimização — cobertos pelas Tasks 4 e 5.

- [ ] **Step 3: Commit final proposto após aprovação**

```powershell
git add docs/superpowers/plans/2026-08-30-account-deactivation-recovery-anonymization.md
git commit -m "docs(account): record GAM-14 implementation verification"
```

## Segurança e critérios de não-regressão

- A autorização self-service continua a verificar o ID persistido no token contra o ID do recurso (`IsCurrentUser`); a desactivação nunca opera sobre outra conta.
- `ActiveUserAuthorizationHandler` continua a bloquear qualquer pedido protegido assim que `isActive = false` ou o `AuthVersion` do token diverge — este plano não altera esse handler, só passa a exercitá-lo de forma legítima.
- Nenhuma password, token, código de recuperação ou dado pessoal é escrito em logs; os erros de anonimização/sweep só registam a contagem processada.
- O cookie de sessão continua `HttpOnly`/`Secure`/`SameSite=Lax`; a desactivação limpa-o explicitamente, tal como o logout.
- As mensagens dos endpoints de recuperação de conta nunca distinguem "conta inexistente" de "já activa"/"já anonimizada"/"prazo expirado" — mesma convenção anti-enumeração do `PasswordRecoveryService`.
- Não introduzir alterações ao esquema além das cinco colunas descritas, seeds permanentes, dados reais, endpoints externos ou dependências novas.

## Handoff

Ordem recomendada: Tasks 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8, com revisão após cada commit. A Task 6 depende da Task 4 (o teste de isolamento substituto tem de existir antes de remover os antigos). A Task 5 (anonimização) é independente da Task 7 (frontend) e pode ser feita em paralelo por um subagente isolado, se o fluxo Explorar → Planear → Implementar for delegado. A execução pode ser feita por subagentes isolados por tarefa ou inline nesta sessão; ambas exigem a verificação final completa (Task 8) antes de qualquer commit final. **Antes de produção**, validar com o utilizador a suposição sobre ausência de obrigações de retenção legal assinalada na secção Segurança.
