# Design — Estados editoriais, permissões e validações de quiz (GAM-45)

## Objetivo

Substituir o antigo campo `Quizz.IsPublished` por uma máquina de estados
editorial validada no backend. Publicar deixa de ser uma simples alteração de
campo: exige validação do conteúdo, e o conteúdo de quizzes publicados ou com
tentativas fica protegido contra alterações.

Este documento substitui as referências a `IsPublished` em
[`2026-08-19-quiz-mvp-design.md`](2026-08-19-quiz-mvp-design.md).

## Estados

O estado é guardado como inteiro (`QuizStatus`), tal como os restantes enums,
e é serializado como inteiro na API.

| Valor | Estado | Etiqueta na UI |
|---|---|---|
| `0` | `Draft` | Rascunho |
| `1` | `Published` | Publicado |
| `2` | `Unpublished` | Retirado |
| `3` | `Archived` | Arquivado |

A migração `ReplaceQuizPublicationWithStatus` converte os dados existentes:
`IsPublished = true` passa a `Published` e `IsPublished = false` passa a
`Draft`. O `Down` faz o inverso (`Published` → `IsPublished = true`; os
restantes estados → `false`).

## Transições

Só são aceites as transições seguintes. Qualquer outra (incluindo manter o
mesmo estado) devolve `409` com a mensagem
`"Cannot change quiz status from X to Y."`.

| De → Para | Condição |
|---|---|
| Draft → Published | o quiz passa a validação de publicação |
| Unpublished → Published | o quiz passa a validação de publicação |
| Published → Unpublished | sempre |
| Draft → Archived | sempre |
| Unpublished → Archived | sempre |
| Archived → Unpublished | sempre (restaurar) |

Um quiz publicado tem de ser retirado antes de ser arquivado. Um quiz
arquivado só pode voltar a Retirado.

A mudança de estado decorre dentro de uma transação, com a linha do quiz
bloqueada (`SELECT … FOR UPDATE`), para evitar condições de corrida entre
publicar e editar.

## Criação

Um quiz novo começa sempre em `Draft`. O estado não é aceite no corpo de
criação nem de atualização (o campo `status` do DTO é apenas de saída e é
ignorado na entrada); só muda através do endpoint de estado.

## Validação de publicação

Antes de passar a `Published`, o quiz tem de cumprir todas as regras abaixo.
A primeira falha devolve `400` com uma mensagem específica, em inglês, sem
dados internos.

| Regra | Mensagem |
|---|---|
| Título preenchido | `A quiz title is required.` |
| Título com no máximo 100 caracteres | `The quiz title cannot exceed 100 characters.` |
| Dificuldade definida | `The quiz difficulty is invalid.` |
| Pelo menos uma pergunta | `A quiz must have at least one question to be published.` |
| Cada pergunta passa os validadores existentes (forma, opções, resposta correta) | `Question N: <mensagem do validador>` |
| Perguntas `TRUEORFALSE` têm exatamente 2 opções | `Question N: A true or false question must have exactly two options.` |

`N` é a posição da pergunta no quiz (a partir de 1, pela ordem de criação).
As mensagens dos validadores existentes são:

- forma: `A question, supported answer type, options and correct answer are required.`
  ou `A question or its options exceed the allowed size.`;
- opções: `Each question option must be unique and non-empty.`;
- resposta correta: `The correct answer must be one of the question options.`.

## Proteção do conteúdo

Criar, editar ou apagar perguntas, e o `PUT` do quiz (título, dificuldade ou
perguntas), só são permitidos quando:

1. o quiz está em `Draft` ou `Unpublished`; caso contrário, `409`
   `"Only draft or unpublished quizzes can be edited."`;
2. o quiz não tem tentativas (`Score`); caso contrário, `409`
   `"A quiz with existing attempts cannot have its content changed."`.

As verificações são feitas depois de bloquear a linha do quiz. Não há
versionamento: um quiz com tentativas fica com o conteúdo congelado, para que
o histórico de pontuações mantenha o significado. A regra existente de não
apagar um quiz com tentativas (`409`) mantém-se.

## Apagar quizzes

Um quiz em `Published` não pode ser apagado: depois de bloquear a linha, o
`DELETE` devolve `409` `"Unpublish the quiz before deleting it."` e o quiz
mantém-se. Quizzes em `Draft`, `Unpublished` ou `Archived` sem tentativas são
apagados (`204`); com tentativas continuam a devolver `409`
`"A quiz with existing attempts cannot be deleted."`.

## Serialização entre tentativas e edição

A submissão de uma tentativa corre numa transação curta:

1. bloqueia a linha do quiz com `SELECT … FOR SHARE` (várias tentativas podem
   correr em paralelo, mas esperam pelo `FOR UPDATE` de uma operação de
   administração e bloqueiam-no);
2. volta a ler o quiz e as perguntas dentro da transação, filtrando por
   `Published`;
3. valida, calcula a pontuação, grava o `Score` e faz commit.

Antes do `FOR SHARE` a transação executa `SET LOCAL lock_timeout = '5s'`. Se o
Postgres devolver `55P03` (`lock_not_available`, possivelmente embrulhado pelo
EF), a transação faz rollback, nada é gravado e a API responde `409`
`{ "message": "The quiz is being updated. Please try again." }`.

Assim uma tentativa e uma edição ou mudança de estado ficam serializadas: ou a
tentativa grava primeiro e a proteção de conteúdo passa a ver o `Score`, ou a
tentativa espera e, se o quiz deixou de estar publicado, devolve `404` sem
gravar pontuação. Ambas as partes bloqueiam apenas a mesma linha de `Quizzs`,
pela mesma ordem, pelo que não há risco de deadlock.

## Catálogo público

A listagem, o detalhe e a submissão de tentativas só consideram quizzes com
`Status == Published`. Quizzes em rascunho, retirados ou arquivados não
aparecem e devolvem `404` no detalhe e na tentativa.

## Autorização

Todas as rotas `api/admin/quizzes` e `api/admin/questions`, incluindo o
endpoint de estado, estão sob a policy `ActiveAdmin` (utilizador autenticado
com role `Admin` e conta ativa, verificado na base de dados). Um utilizador
normal ou um administrador inativo recebe `403`. Qualquer administrador ativo
pode gerir qualquer quiz.

## Contrato da API de administração

- `GET /api/admin/quizzes?page=1&pageSize=50&includeArchived=false`: lista
  quizzes. Por omissão exclui os arquivados; `includeArchived=true` inclui-os.
- `GET /api/admin/quizzes/{id}`: devolve o quiz com perguntas (qualquer
  estado).
- `POST /api/admin/quizzes`: cria um quiz em `Draft`.
- `PUT /api/admin/quizzes/{id}`: altera título, dificuldade e, opcionalmente,
  perguntas, sujeito à proteção de conteúdo.
- `DELETE /api/admin/quizzes/{id}`: apaga um quiz sem tentativas.
- `POST /api/admin/quizzes/{id}/status` com corpo `{ "status": <0-3> }`:
  muda o estado e devolve `200` com o quiz atualizado.
- `POST /api/admin/quizzes/{quizId}/questions`,
  `PUT /api/admin/questions/{id}` e `DELETE /api/admin/questions/{id}`:
  gerem perguntas, sujeitos à proteção de conteúdo.

O DTO de quiz devolvido inclui `status` (inteiro) no lugar de `isPublished`,
e `hasAttempts` (booleano), que indica se o quiz já tem tentativas. Ambos são
apenas de saída e são ignorados no pedido. `hasAttempts` está presente na
lista (calculado numa única query, sem N+1), no detalhe e nas respostas de
criação, atualização e mudança de estado.

### Erros

As respostas `400`, `404` e `409` das operações de administração devolvem
`{ "message": "<texto>" }`. As mensagens são textos fixos definidos no código
e não incluem identificadores de outros utilizadores, stack traces nem SQL.
Um estado ausente, `null` ou fora dos valores definidos devolve `400`
`"A valid quiz status is required."`.

### Erros de validação automática (toda a API)

Os `400` automáticos dos controladores `[ApiController]` deixam de devolver
`ValidationProblemDetails` e passam a devolver `{ "message": "<texto>" }` em
toda a API (`InvalidModelStateResponse`):

- se o corpo for JSON mal formado ou tiver um tipo errado (erro do parser,
  com chave começada por `$` ou com exceção associada), a mensagem é fixa:
  `"The request body is invalid."`, para não expor nomes de tipos .NET;
- se alguma entrada inválida tiver `AttemptedValue` (erros de binding de rota
  ou query, p. ex. `?page=abc`), a mensagem é fixa `"The request is invalid."`,
  para não ecoar o valor enviado pelo cliente;
- se a falha vier de DataAnnotations do corpo (sem `AttemptedValue`), usa-se a
  primeira mensagem de validação.

## Interface de administração

- A lista mostra a etiqueta do estado de cada quiz e apenas os botões das
  transições permitidas: Publicar, Retirar, Arquivar e Restaurar.
- A opção "Mostrar arquivados" inclui os quizzes arquivados na lista.
- O editor carrega o quiz por `GET /api/admin/quizzes/{id}`, mostra o estado
  e desativa a edição fora de Rascunho/Retirado, com uma explicação.
- Quando `hasAttempts` é verdadeiro, o editor desativa a edição desde o início
  e mostra "Este quiz já tem tentativas e o conteúdo não pode ser alterado.
  Podes retirá-lo ou arquivá-lo.".
- A lista mostra o indicador "Com tentativas" e esconde a ação Apagar nos
  quizzes com tentativas e nos quizzes publicados.
- As mensagens de erro do backend são mostradas ao administrador.

## Testes

As regras estão cobertas por testes unitários (matriz de transições e
validação de publicação) e de integração (endpoint de estado, autorização,
proteção de conteúdo e filtragem do catálogo).
