## v0.12.5 - 2026-09-07
* fix(ci): wait for the API to be ready before running Docker E2E tests
## v0.12.4 - 2026-09-07
* fix(ci): detect the migrate container's exit status in Docker E2E job
## v0.12.3 - 2026-09-07
* fix(deps): resolve npm audit vulnerabilities in frontend lockfile
## v0.12.2 - 2026-09-07
* fix(auth): repair social login and add account linking, password toggle
* fix(security): require an explicit AllowedHosts outside Development (GAM-52)
* feat(account): replace deletion with deactivation, recovery, and anonymization (GAM-14)
* feat(account): add secure email and password change flows
* fix(docker): install missing GSSAPI library in production image
* docs: plan to reduce cyclomatic_complexity
* refactor(complexity): split high-complexity flows
* ci(testing): enforce coverage and Docker E2E
* test(frontend): add coverage and E2E flows
* test(backend): expand security and quiz coverage
* fix(security): harden auth and account flows
* ci: sync versioning workflows to 2.9.0
## v0.10.6 - 2026-08-22
* fix(security): harden dependencies and Docker config
* fix(account): secure user profile flows
## v0.10.4 - 2026-08-19
* fix(admin): deny inactive admin sessions
* fix(quiz): include quiz ID in result route
* fix(quiz): reject empty quiz attempts
* fix(docker): build backend project in containers
* feat(admin): add quiz management UI
* feat(quiz): add player quiz flow
* test(frontend): cover authenticated navigation
* feat(admin): manage curated quizzes
* feat(quiz): score authenticated attempts
* feat(quiz): add protected published catalog
* feat(auth): authorize admin roles
* feat(auth): add persistent admin role
* test(api): add PostgreSQL integration harness
* chore: ignore local orchestration artifacts
* docs(quiz): add implementation plan
* docs(quiz): define MVP flow
## v0.3.0 - 2026-08-19
* feat(docker): add Compose environments
* ci: update do ficheiro de controlo de versões
## v0.2.9 - 2026-08-18
* chore(config): validate required runtime configuration
* chore(ci): update semantic versioning workflow via automatic-version-control
* ci: update do ficheiro de controlo de versões
## v0.2.6 - 2026-04-10
* fix: corrigido erros de negócio
## v0.2.5 - 2026-04-09
* fix: corrigido bugs
## v0.2.4 - 2026-04-09
* ci: corrigido um erro no ficheiro de versionamento
## v0.2.3 - 2026-02-09
* ci: corrigido erro na pipeline
## v0.1.6 - 2026-02-07
* fix: corrigidos erros em geral e o layout do sistema, simplificado código e  aumentado a reutilização do código
## v0.1.5 - 2026-02-05
* fix: corrigido vários erros
## v0.1.4 - 2026-02-04
* fix: corrigido uns erros no JWT e upgrade para .dotnet 10.0
## v0.1.3 - 2026-02-02
* ci: corrigido uns erros no versioning
## v0.1.2 - 2026-02-01
* ci: versão definitiva do versioning
* chore(release): v0.1.1
## v0.1.1 - 2026-02-01
* ci: atualizado controlo de versões
* chore: update release notes and changelog for v0.1.0
## v0.1.0 - 2025-08-12
* Merge branch 'dev'
* fix: corrigido bugs na recuperação de password
* Corrigido alguns erros
* Terminado a documentação de código do backend
* Começado a documentar código
* Criado documentação inicial do projeto
* fix: corrigido um bug na sessão de reset password
* fix: Transformado o código em algo mais modular
* fix: corrigido alguns erros e tornado o código mais modular
* protegendo rotas e iniciado no frontend como resetar password em caso de perda
* fix: corrigido alguns bugs existentes no profile
* Criado token de acesso, tornando o site mais seguro
* criado apenas no backend forma de recuperar a password em caso de a perder
* Criado login com google e github
* fix: melhorado certos elementos visuais
* fix: corrigido alguns erros no backend e melhorado o registo de utilizadores
* Iniciado a criação de registo via google
* fix: corrigido alguns bugs
* feat: Iniciado frontend em vue.js
* fit: Ajuste efetuados no backend
* continuação do ultimo commit
* fit: Criado os Mappers e corrigido uns erros
* fit: Criado os Dtos e feito alguns ajustes nas classes base
* chore: update release notes and changelog for v0.0.2
* fit: Criado as classes bases para guardar os dados no backend
## v0.0.2 - 2024-12-21
* Update versioning.yml
* chore: update release notes and changelog for v0.0.1
## v0.0.1 - 2024-12-19
* fit: Iniciado o backend com Users e Quizz
* chore: update release notes and changelog for v0.0.1
* Update versioning.yml
## v0.0.1 - 2024-12-19
* Update versioning.yml
