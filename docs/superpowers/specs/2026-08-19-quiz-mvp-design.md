# Design — MVP de quizzes e administração

## Objetivo

Entregar o primeiro percurso completo autenticado do produto: um utilizador
consulta quizzes curados publicados, responde uma tentativa e recebe o seu
resultado. Um administrador gere esse conteúdo numa área restrita. O mesmo
incremento introduz a base de testes automatizados para estes comportamentos.

## Âmbito e acesso

- A landing page e os seus destaques mantêm-se públicos.
- Todas as rotas de catálogo, realização de quiz, resultado e administração
  exigem JWT válido.
- Há duas roles persistentes: `User` e `Admin`.
- O primeiro administrador é criado de forma idempotente no arranque da API,
  a partir de `INITIAL_ADMIN_EMAIL` e `INITIAL_ADMIN_PASSWORD`. Se o email já
  existir, esse utilizador é promovido a `Admin`; caso contrário, é criado com
  password BCrypt. Os valores reais não são versionados nem registados; em
  produção vêm de um gestor de segredos ou CI.
- Apenas `Admin` pode criar, alterar, publicar, despublicar ou eliminar
  quizzes e perguntas.

## Modelo de dados

- `User` ganha a role persistente, com `User` como valor por defeito. A role
  é incluída no JWT e no DTO devolvido no login, mas nunca são devolvidas
  passwords, hashes ou variáveis de bootstrap.
- `Quizz` ganha `IsPublished`; apenas quizzes publicados aparecem no catálogo
  de utilizadores. Na criação, o servidor define o autor como o administrador
  autenticado; o número de perguntas é mantido pelo servidor para não divergir
  do conteúdo efetivo.
- `Question` permanece associado a um quiz. A resposta correta nunca é
  devolvida pelos contratos destinados a participantes.
- Cada submissão cria um `Score`: `QuizzId` preenchido, `GameId` nulo, data
  UTC e `Points` igual ao número de respostas certas. XP, rankings e alteração
  de `User.TotalPoints` ficam fora deste incremento.

## API

### Participante autenticado

- `GET /api/quizzes`: devolve o catálogo de quizzes publicados, sem perguntas
  nem respostas corretas.
- `GET /api/quizzes/{id}`: devolve um quiz publicado com perguntas, opções e
  tipo de resposta, mas sem respostas corretas.
- `POST /api/quizzes/{id}/attempts`: recebe uma resposta por pergunta, valida
  que o quiz está publicado e que os IDs pertencem ao quiz, calcula a pontuação
  no servidor e devolve `correctAnswers`, `totalQuestions` e `percentage`.

Respostas duplicadas, perguntas em falta, opções que não pertencem à pergunta,
IDs inválidos e quizzes não publicados devolvem `400` ou `404` sem criar score.
Pedidos sem JWT devolvem `401`.

### Administração

- `GET /api/admin/quizzes`, `POST /api/admin/quizzes`,
  `PUT /api/admin/quizzes/{id}` e `DELETE /api/admin/quizzes/{id}` gerem
  quizzes, incluindo o estado de publicação.
- `POST /api/admin/quizzes/{quizId}/questions`,
  `PUT /api/admin/questions/{id}` e `DELETE /api/admin/questions/{id}` gerem
  perguntas.
- Todos os endpoints administrativos usam `Authorize(Roles = "Admin")` e
  devolvem `403` para utilizadores autenticados sem essa role.

Os pedidos administrativos validam título não vazio até 100 caracteres,
perguntas e opções não vazias, resposta correta presente nas opções e tipo de
resposta suportado. A criação ou alteração de conteúdo é transacional quando
envolve o quiz e as suas perguntas.

## Frontend

- Adicionar rotas protegidas `/quizzes`, `/quizzes/:id`,
  `/quizzes/:id/result` e `/admin/quizzes`.
- O catálogo apresenta apenas dados do contrato público autenticado e abre a
  realização do quiz.
- A vista de quiz recolhe uma resposta por pergunta e apresenta o resultado
  devolvido pela API; não calcula nem contém respostas corretas.
- A área de administração apresenta lista, editor de quiz e editor de
  perguntas. A navegação só mostra administração quando a role do JWT/user
  autenticado é `Admin`; a API continua a ser a barreira de segurança.

## Testes e validação

- Criar um projeto xUnit de integração para o backend com
  `WebApplicationFactory` e PostgreSQL temporário através de Testcontainers.
  A infraestrutura reproduz a base de dados real sem reutilizar dados locais.
- Cobrir criação idempotente do administrador, emissão de JWT com role,
  bloqueio `401`/`403`, catálogo sem respostas corretas, submissão de tentativa
  válida e rejeição de submissões inválidas, e CRUD administrativo.
- Configurar Vitest no frontend e testar o guard de rotas e os composables que
  tratam catálogo, submissão e resultado.
- A validação final inclui `dotnet test`, testes Vitest, builds Docker dev/prod
  e smoke tests HTTP do catálogo autenticado e da área administrativa.

## Fora de âmbito

- Gestão de utilizadores por administradores.
- Estatísticas, rankings, XP, conquistas e limite de tentativas.
- Upload de imagens, quizzes criados por utilizadores e uma aplicação de
  administração separada.
