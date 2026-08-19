# Docker and Docker Compose Environment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Disponibilizar um ambiente Docker Compose reproduzível para desenvolvimento local e imagens de produção do frontend, API e PostgreSQL.

**Architecture:** `compose.yml` na raiz define exclusivamente os serviços comuns (PostgreSQL persistente e executor EF Core). `compose.dev.yml` define o desenvolvimento com hot reload e `compose.prod.yml` define as imagens finais. Os ambientes usam os mesmos serviços comuns, mas são iniciados com ficheiros Compose explicitamente selecionados; em produção, Nginx serve o frontend compilado e encaminha `/api/` para a API na rede interna.

**Tech Stack:** Docker Engine 29, Docker Compose v5, .NET 10, EF Core 10, Node.js 22, Vite 7, Nginx e PostgreSQL 17.

## Global Constraints

- Não adicionar dependências ao projeto; `dotnet-ef` 10.0.2 existe apenas no estágio de migração da imagem e corresponde à versão EF Core já usada pelo backend.
- Nunca versionar valores reais: `.env` fica ignorado e `.env.example` contém exclusivamente valores fictícios.
- O `.env` é exclusivamente local; em produção, `DATABASE_CONNECTION_STRING` e os restantes segredos são fornecidos por gestor de segredos, CI ou `--env-file` fora do repositório.
- Não abrir nem alterar `GameSphere_backend/appsettings.json` ou `GameSphere_backend/.env`, que são locais e ignorados.
- Não executar migrações destrutivas nem remover o volume PostgreSQL automaticamente; o volume só é removido por ação explícita do utilizador.
- Os ambientes são executados separadamente: `compose.dev.yml` para hot reload e `compose.prod.yml` para imagens finais. Não combinar ambos no mesmo comando Compose.
- Não alterar CORS, autenticação, controladores, schema ou a lógica de negócio existente.
- A validação inclui compilação nativa, validação da configuração Compose e arranque funcional dos dois perfis. Não existe atualmente projeto de testes backend nem script npm de testes.

## Segurança

- A única entrada externa de configuração é o ficheiro local `.env`; o Compose falha se uma variável obrigatória faltar e não aceita defaults secretos no YAML.
- As credenciais da base de dados, JWT e SMTP são injetadas por variáveis de ambiente do processo e não são copiadas para nenhuma imagem.
- Os contextos de build excluem `node_modules`, artefactos, configurações locais e segredos, evitando cópias acidentais para as imagens.
- A API de produção não publica uma porta no host; é alcançável apenas pelo proxy Nginx na rede Compose. A porta da base de dados também não é publicada.
- A imagem final da API executa como o utilizador não privilegiado `app` já fornecido pela imagem oficial ASP.NET.

## Estrutura de ficheiros

| Ficheiro | Responsabilidade única |
| --- | --- |
| `.env.example` | Documentar as variáveis de execução com valores estritamente fictícios. |
| `.gitignore` | Impedir que o `.env` da raiz entre no controlo de versão. |
| `compose.yml` | Orquestrar exclusivamente a base de dados e a migração comuns aos dois ambientes. |
| `compose.dev.yml` | Orquestrar a API e o frontend de desenvolvimento com hot reload. |
| `compose.prod.yml` | Orquestrar as imagens de produção, incluindo o proxy Nginx. |
| `GameSphere_backend/.dockerignore` | Restringir o contexto de build da API a fontes não sensíveis. |
| `GameSphere_backend/Dockerfile` | Construir os targets `development`, `migration` e `production` da API. |
| `GameSphere_frontend/.dockerignore` | Restringir o contexto de build do frontend. |
| `GameSphere_frontend/Dockerfile` | Construir os targets `development` e `production` do frontend. |
| `GameSphere_frontend/nginx/default.conf` | Servir a SPA de produção e encaminhar `/api/` internamente. |
| `GameSphere_frontend/src/services/api.js` | Obter o URL base da API da configuração Vite, com o URL atual como fallback local. |
| `README.md` | Explicar pré-requisitos, configuração, perfis, validação e preservação de dados. |

## Padrão arquitetural

Aplicar a convenção orientada a serviços do Docker Compose. Cada contentor tem uma única responsabilidade: `db` armazena dados, `migrate` aplica migrações e termina, `api-*` executa a API e `frontend-*` entrega a interface. Esta separação evita acoplar migrações ao arranque da aplicação e impede corridas de migração quando existirem múltiplas réplicas numa evolução futura.

---

### Task 1: Configuração local segura e contextos de build

**Files:**

- Create: `.env.example`
- Create: `GameSphere_backend/.dockerignore`
- Create: `GameSphere_frontend/.dockerignore`
- Modify: `.gitignore`

**Interfaces:**

- Consumes: as chaves de configuração obrigatórias indicadas em `GameSphere_backend/appsettings.example.json`.
- Produces: um contrato de variáveis consumido por `compose.yml`; apenas `.env.example` pode ser versionado.

- [ ] **Step 1: Criar o contrato de variáveis com valores fictícios**

Criar `.env.example` com este conteúdo. Antes do arranque, copiar para `.env` e substituir todos os valores `CHANGE_ME` localmente. `DATABASE_CONNECTION_STRING` é uma connection string Npgsql completa para o serviço `db`; em produção, deve vir já formatada do gestor de segredos ou CI.

```dotenv
POSTGRES_DB=gamesphere
POSTGRES_USER=gamesphere
POSTGRES_PASSWORD=CHANGE_ME_LOCAL_DATABASE_PASSWORD
DATABASE_CONNECTION_STRING=Host=db;Port=5432;Database=gamesphere;Username=gamesphere;Password=CHANGE_ME_LOCAL_DATABASE_PASSWORD

JWT_SECRET=CHANGE_ME_WITH_A_LOCAL_SECRET_AT_LEAST_32_BYTES_LONG
JWT_ISSUER=GameSphere
JWT_AUDIENCE=GameSphere.Client

SMTP_SERVER=smtp.example.test
SMTP_PORT=587
SMTP_SENDER_EMAIL=noreply@example.test
SMTP_SENDER_NAME=GameSphere Support
SMTP_USERNAME=CHANGE_ME_LOCAL_SMTP_USERNAME
SMTP_PASSWORD=CHANGE_ME_LOCAL_SMTP_PASSWORD
SMTP_ENABLE_SSL=true
```

- [ ] **Step 2: Ignorar o ficheiro local de Compose**

Adicionar o seguinte bloco no fim de `.gitignore`:

```gitignore
# Docker Compose local environment
/.env
/.env.*
!/.env.example
```

- [ ] **Step 3: Excluir ficheiros não necessários dos builds**

Criar `GameSphere_backend/.dockerignore`:

```gitignore
bin/
obj/
.vs/
.idea/
.env
.env.*
!.env.example
appsettings.json
appsettings.*.json
!appsettings.example.json
```

Criar `GameSphere_frontend/.dockerignore`:

```gitignore
node_modules/
dist/
storybook-static/
.fleet/
debug-storybook.log
npm-debug.log*
.env
.env.*
!.env.example
```

- [ ] **Step 4: Verificar que não há segredos no diff**

Run: `git diff --check; git diff -- .gitignore .env.example GameSphere_backend/.dockerignore GameSphere_frontend/.dockerignore`

Expected: sem espaços inválidos e sem ficheiros locais `.env`, `appsettings.json` ou valores reais de credenciais no diff.

### Task 2: Construir a imagem multi-stage da API e o executor de migrações

**Files:**

- Create: `GameSphere_backend/Dockerfile`

**Interfaces:**

- Consumes: `GameSphere_backend.csproj`, as migrações EF Core existentes e as variáveis `ConnectionStrings__GameSphereDB`, `JwtSettings__*` e `EmailSettings__*`.
- Produces: targets Docker `development`, `migration` e `production`, todos a escutar HTTP na porta interna `8080` quando executam a API.

- [ ] **Step 1: Criar a imagem da API**

Criar `GameSphere_backend/Dockerfile` com os targets abaixo. O target `migration` compila a aplicação em Release e aplica apenas as migrações já versionadas; não cria novas migrações.

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /src

COPY GameSphere_backend.csproj ./
RUN dotnet restore

COPY . ./

FROM base AS development
EXPOSE 8080
ENTRYPOINT ["dotnet", "watch", "run", "--urls", "http://0.0.0.0:8080"]

FROM base AS build
RUN dotnet publish -c Release --no-restore -o /app/publish

FROM base AS migration
RUN dotnet build -c Release --no-restore
RUN dotnet tool install --tool-path /tools dotnet-ef --version 10.0.2
ENV PATH="/tools:${PATH}"
ENTRYPOINT ["dotnet-ef", "database", "update", "--no-build", "--configuration", "Release", "--project", "GameSphere_backend.csproj"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS production
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "GameSphere_backend.dll"]
```

- [ ] **Step 2: Construir cada target sem iniciar serviços**

Run: `docker build --target development -t gamesphere-api:development GameSphere_backend`

Expected: imagem criada e nenhum ficheiro local de configuração listado no output de `COPY`.

Run: `docker build --target migration -t gamesphere-api:migration GameSphere_backend`

Expected: imagem criada com `dotnet-ef` 10.0.2.

Run: `docker build --target production -t gamesphere-api:production GameSphere_backend`

Expected: imagem criada, com artefactos publicados e sem SDK no estágio final.

### Task 3: Tornar configurável o cliente HTTP e criar a imagem do frontend

**Files:**

- Modify: `GameSphere_frontend/src/services/api.js`
- Create: `GameSphere_frontend/Dockerfile`
- Create: `GameSphere_frontend/nginx/default.conf`

**Interfaces:**

- Consumes: `VITE_API_BASE_URL` no processo Vite; quando ausente, mantém `http://localhost:5095/api`.
- Produces: `frontend-dev` servido em `5173` e `frontend-prod` servido por Nginx em `80`, com pedidos `/api/` encaminhados para `api-prod:8080`.

- [ ] **Step 1: Substituir o URL rígido da API por configuração Vite**

Substituir todo o conteúdo de `GameSphere_frontend/src/services/api.js` por:

```javascript
import axios from 'axios';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5095/api';

const api = axios.create({
  baseURL: apiBaseUrl,
  timeout: 15000,
});

export default api;
```

- [ ] **Step 2: Criar a imagem multi-stage do frontend**

Criar `GameSphere_frontend/Dockerfile`:

```dockerfile
FROM node:22-alpine AS dependencies
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

FROM dependencies AS development
COPY . ./
EXPOSE 5173
CMD ["sh", "-c", "npm ci && npm run dev -- --host 0.0.0.0"]

FROM dependencies AS build
COPY . ./
ARG VITE_API_BASE_URL=/api
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
RUN npm run build

FROM nginx:1.27-alpine AS production
COPY nginx/default.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
```

- [ ] **Step 3: Criar o proxy Nginx da imagem de produção**

Criar `GameSphere_frontend/nginx/default.conf`:

```nginx
server {
    listen 80;
    server_name _;
    root /usr/share/nginx/html;
    index index.html;

    location /api/ {
        proxy_pass http://api-prod:8080/api/;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

- [ ] **Step 4: Verificar a compilação do frontend e os targets Docker**

Run: `npm run build`

Working directory: `GameSphere_frontend`

Expected: Vite termina com sucesso e cria `dist/` local, que já é ignorado.

Run: `docker build --target development -t gamesphere-frontend:development GameSphere_frontend`

Expected: imagem criada com Vite acessível na porta interna `5173`.

Run: `docker build --target production --build-arg VITE_API_BASE_URL=/api -t gamesphere-frontend:production GameSphere_frontend`

Expected: imagem criada com os assets estáticos e a configuração Nginx.

### Task 4: Orquestrar ambientes Compose separados e a persistência PostgreSQL

**Files:**

- Create: `compose.yml`
- Create: `compose.dev.yml`
- Create: `compose.prod.yml`

**Interfaces:**

- Consumes: `.env`, os targets `development`, `migration` e `production`, e `VITE_API_BASE_URL`.
- Produces: `docker compose -f compose.yml -f compose.dev.yml up --build` e `docker compose -f compose.yml -f compose.prod.yml up --build`.

- [ ] **Step 1: Criar os ficheiros Compose com serviços isolados por responsabilidade**

Criar `compose.yml` apenas com `db`, `migrate`, `postgres-data` e `x-api-environment`. Criar `compose.dev.yml` com `api-dev`, `frontend-dev` e os volumes de desenvolvimento; criar `compose.prod.yml` com `api-prod` e `frontend-prod`. Não usar `profiles` e não duplicar `db` nem `migrate` nos ficheiros específicos de ambiente.

```yaml
name: gamesphere

x-api-environment: &api-environment
  ASPNETCORE_ENVIRONMENT: Development
  ASPNETCORE_URLS: http://+:8080
  ConnectionStrings__GameSphereDB: ${DATABASE_CONNECTION_STRING:?DATABASE_CONNECTION_STRING is required}
  JwtSettings__SecretKey: ${JWT_SECRET:?JWT_SECRET is required}
  JwtSettings__Issuer: ${JWT_ISSUER:?JWT_ISSUER is required}
  JwtSettings__Audience: ${JWT_AUDIENCE:?JWT_AUDIENCE is required}
  EmailSettings__SmtpServer: ${SMTP_SERVER:?SMTP_SERVER is required}
  EmailSettings__SmtpPort: ${SMTP_PORT:?SMTP_PORT is required}
  EmailSettings__SenderEmail: ${SMTP_SENDER_EMAIL:?SMTP_SENDER_EMAIL is required}
  EmailSettings__SenderName: ${SMTP_SENDER_NAME:?SMTP_SENDER_NAME is required}
  EmailSettings__Username: ${SMTP_USERNAME:?SMTP_USERNAME is required}
  EmailSettings__Password: ${SMTP_PASSWORD:?SMTP_PASSWORD is required}
  EmailSettings__EnableSSL: ${SMTP_ENABLE_SSL:?SMTP_ENABLE_SSL is required}

services:
  db:
    image: postgres:17-alpine
    restart: unless-stopped
    environment:
      POSTGRES_DB: ${POSTGRES_DB:?POSTGRES_DB is required}
      POSTGRES_USER: ${POSTGRES_USER:?POSTGRES_USER is required}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is required}
    healthcheck:
      test: ['CMD-SHELL', 'pg_isready -U "$$POSTGRES_USER" -d "$$POSTGRES_DB"']
      interval: 5s
      timeout: 5s
      retries: 12
      start_period: 5s
    volumes:
      - postgres-data:/var/lib/postgresql/data

  migrate:
    build:
      context: ./GameSphere_backend
      target: migration
    environment: *api-environment
    depends_on:
      db:
        condition: service_healthy
    restart: "no"

volumes:
  postgres-data:
```

Em `compose.dev.yml`, manter `ConnectionStrings__GameSphereDB` como `${DATABASE_CONNECTION_STRING:?DATABASE_CONNECTION_STRING is required}`, as restantes variáveis da API, os targets `development`, as dependências de `migrate` e `api-dev`, as portas locais `127.0.0.1:5095:8080` e `127.0.0.1:5173:5173`, e os volumes `nuget-packages` e `frontend-node-modules`. Em `compose.prod.yml`, sobrescrever também `migrate.environment.ASPNETCORE_ENVIRONMENT` para `Production`; manter a mesma connection string e variáveis da API com `ASPNETCORE_ENVIRONMENT: Production`, os targets `production`, as dependências de `migrate` e `api-prod`, e apenas a porta local `127.0.0.1:8080:80` do frontend.

- [ ] **Step 2: Validar a configuração antes de criar contentores**

Run: `Copy-Item .env.example .env`

Expected: existe `.env` apenas localmente e `git status --short` não o mostra.

Run: `docker compose -f compose.yml -f compose.dev.yml config --quiet`

Expected: termina com código 0 e não imprime variáveis de segredo.

Run: `docker compose -f compose.yml -f compose.prod.yml config --quiet`

Expected: termina com código 0 e não imprime variáveis de segredo.

- [ ] **Step 3: Arrancar e validar o perfil de desenvolvimento**

Run: `docker compose -f compose.yml -f compose.dev.yml up --build --detach`

Expected: `db`, `migrate`, `api-dev` e `frontend-dev` arrancam; `migrate` termina com código 0.

Run: `docker compose -f compose.yml -f compose.dev.yml ps`

Expected: `db`, `api-dev` e `frontend-dev` estão em execução; `migrate` aparece concluído com código 0.

Run: `Invoke-WebRequest http://localhost:5173 -UseBasicParsing | Select-Object -ExpandProperty StatusCode`

Expected: `200`.

Run: `Invoke-WebRequest http://localhost:5095/swagger/index.html -UseBasicParsing | Select-Object -ExpandProperty StatusCode`

Expected: `200` em desenvolvimento.

- [ ] **Step 4: Parar o perfil de desenvolvimento sem apagar dados**

Run: `docker compose -f compose.yml -f compose.dev.yml down`

Expected: os contentores e a rede são removidos; o volume `postgres-data` mantém-se porque o comando não usa `--volumes`.

- [ ] **Step 5: Arrancar e validar o perfil de produção**

Run: `docker compose -f compose.yml -f compose.prod.yml up --build --detach`

Expected: `db`, `migrate`, `api-prod` e `frontend-prod` arrancam; nenhuma porta da API ou PostgreSQL é exposta no host.

Run: `Invoke-WebRequest http://localhost:8080 -UseBasicParsing | Select-Object -ExpandProperty StatusCode`

Expected: `200`.

Run: `try { Invoke-WebRequest http://localhost:8080/api/User/login -Method POST -ContentType 'application/json' -Body '{"email":"invalid@example.test","password":"invalid"}' -UseBasicParsing } catch { $_.Exception.Response.StatusCode.value__ }`

Expected: uma resposta HTTP da API, tipicamente `400` ou `401`; não pode ser `502`, o que provaria que o Nginx não alcançou a API.

- [ ] **Step 6: Documentar a atualização de schema durante desenvolvimento**

Depois de criar uma nova migração EF Core, executar explicitamente:

Run: `docker compose -f compose.yml -f compose.dev.yml run --rm migrate`

Expected: a migração pendente é aplicada uma vez ao volume PostgreSQL existente, sem apagar dados.

### Task 5: Documentar a utilização e validar os artefactos finais

**Files:**

- Modify: `README.md`

**Interfaces:**

- Consumes: os comandos de ambiente definidos por `compose.yml` com `compose.dev.yml` ou `compose.prod.yml`.
- Produces: instruções reproduzíveis para novos contribuidores, sem segredos e sem dependência de ferramentas locais além de Docker Desktop.

- [ ] **Step 1: Acrescentar uma secção `## Ambiente Docker` ao README**

Documentar, em português europeu, exatamente:

```markdown
## Ambiente Docker

Pré-requisito: Docker Desktop com Docker Compose disponível.

1. Copia `.env.example` para `.env` e substitui todos os valores `CHANGE_ME` por valores apenas locais. O `.env` é exclusivamente local; em produção, usa gestor de segredos, CI ou `--env-file` fora do repositório para a connection string Npgsql e restantes segredos.
2. Para desenvolvimento com hot reload, executa `docker compose -f compose.yml -f compose.dev.yml up --build` e abre `http://localhost:5173`. A API e Swagger ficam em `http://localhost:5095/swagger`.
3. Para validação local das imagens de produção, executa `docker compose -f compose.yml -f compose.prod.yml up --build` e abre `http://localhost:8080`. A API não expõe porta pública neste ambiente; é servida em `/api/` pelo Nginx. Este fluxo HTTP em `localhost` não substitui um deployment público com terminação TLS.
4. Para parar sem apagar dados, executa o mesmo comando com `down` em vez de `up --build`. Não uses `--volumes` sem confirmar que podes perder a base de dados local.
5. Depois de adicionares uma migração EF Core, executa `docker compose -f compose.yml -f compose.dev.yml run --rm migrate` para a aplicar à base de dados local.
```

- [ ] **Step 2: Fazer a verificação final**

Run: `dotnet build GameSphere_backend/GameSphere_backend.csproj`

Expected: compilação .NET termina com código 0.

Run: `npm run build`

Working directory: `GameSphere_frontend`

Expected: compilação Vite termina com código 0.

Run: `docker compose -f compose.yml -f compose.dev.yml config --quiet; docker compose -f compose.yml -f compose.prod.yml config --quiet; git diff --check; git status --short`

Expected: os dois perfis são válidos, não há erros de whitespace e apenas os ficheiros planeados aparecem como alterações não commitadas.

- [ ] **Step 3: Rever antes de qualquer commit**

Run: `git diff -- .gitignore .env.example compose.yml compose.dev.yml compose.prod.yml GameSphere_backend/.dockerignore GameSphere_backend/Dockerfile GameSphere_frontend/.dockerignore GameSphere_frontend/Dockerfile GameSphere_frontend/nginx/default.conf GameSphere_frontend/src/services/api.js README.md`

Expected: nenhum segredo, ficheiro local ou artefacto gerado é incluído.

Não criar commit sem um pedido explícito. Se for autorizado após todas as verificações, usar apenas os ficheiros acima e a mensagem `feat(devops): add docker compose environment`.

## Self-review

- **Cobertura:** o plano cria imagens de desenvolvimento e produção para ambos os componentes, orquestra PostgreSQL, aplica migrações antes da API, mantém dados persistentes, remove o URL rígido do cliente e documenta os fluxos de execução.
- **Segurança:** segredos são exclusivamente locais, os contextos de build excluem configurações sensíveis, e o perfil de produção não expõe API nem base de dados no host.
- **Compatibilidade:** não há novas dependências de projeto, não há alterações a schema ou CORS, e o fallback do frontend preserva o URL local atualmente usado fora de Docker.
- **Lacuna conhecida:** não há suite de testes automatizados backend nem comando npm de testes configurado; por isso a validação especificada cobre builds, configuração Compose e smoke tests HTTP dos dois perfis.
