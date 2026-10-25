# Conteúdo curado de quizzes

Quizzes originais em PT-PT (ortografia do AO90), guardados como ficheiros JSON e carregados pela API de administração através de `scripts/load-curated-quizzes.js`. O conteúdo passa sempre pelo fluxo editorial existente: rascunho, validação e publicação.

## Formato

Um ficheiro por quiz:

```json
{
  "title": "Geografia — Capitais da Europa",
  "category": "Geografia",
  "difficulty": "EASY",
  "provenance": { "source": "original", "author": "GameSphere", "reviewedBy": null, "reviewedAt": null },
  "questions": [
    {
      "description": "Qual é a capital de Espanha?",
      "typeOfAnswer": "MULTIPLEOPTION",
      "answers": ["Madrid", "Barcelona", "Sevilha", "Valência"],
      "correctAnswer": "Madrid",
      "explanation": "Madrid é a capital de Espanha."
    }
  ]
}
```

- `title`: 1 a 100 caracteres, sem quebras de linha, e começa por `<category> — ` (a categoria vai no título porque o schema não tem campo de categoria).
- `difficulty`: `EASY`, `MEDIUM` ou `HARD`.
- `questions`: 1 a 100 perguntas. `description` até 2000 caracteres.
- `typeOfAnswer`: `MULTIPLEOPTION` ou `TRUEORFALSE`. Este último tem exatamente 2 respostas (`["Verdadeiro", "Falso"]`).
- `answers`: 2 a 6 respostas, únicas sem distinguir maiúsculas, até 500 caracteres, sem quebras de linha nem caracteres de controlo ou invisíveis. `correctAnswer` é igual a uma delas.
- `explanation`: obrigatória, mas só serve a revisão. O loader ignora-a, tal como `category` e `provenance` (o schema ainda não os guarda).
- `provenance`: `source` é sempre `"original"`. `reviewedBy` e `reviewedAt` são ambos `null` até haver revisão humana e passam a ambos preenchidos depois (`reviewedAt` é uma data real `AAAA-MM-DD`); um preenchido e o outro `null` é inválido. Não incluir dados pessoais nem material protegido.

## Revisão

Quem revê confirma a exatidão dos factos, as respostas e a ortografia, e depois preenche `provenance.reviewedBy` (nome) e `provenance.reviewedAt` (data, `AAAA-MM-DD`) no ficheiro, através de um commit normal. Sem `reviewedBy` e `reviewedAt` preenchidos, o loader recusa publicar.

## Carregamento

Valida os ficheiros, sem rede (termina com código diferente de 0 se algum for inválido, indicando o ficheiro e a pergunta):

```bash
node scripts/load-curated-quizzes.js --dry-run
```

Cria rascunhos dos quizzes que ainda não existem. As credenciais vêm só de variáveis de ambiente e nunca são impressas:

```bash
export GAMESPHERE_ADMIN_EMAIL=...
export GAMESPHERE_ADMIN_PASSWORD=...
export GAMESPHERE_BASE_URL=http://localhost:8080   # valor por omissão
node scripts/load-curated-quizzes.js
```

`GAMESPHERE_BASE_URL` é validado antes de qualquer pedido: tem de ser um URL `http` ou `https` válido, sem credenciais (utilizador ou password) no URL, e `http://` só é aceite para `localhost`, `127.0.0.0/8` e `::1`. Para hosts remotos usa `https://`. Se for inválido, o loader termina com código 1 sem fazer pedidos.

Se um pedido falhar a meio do lote (por exemplo um erro HTTP ao criar ou publicar um quiz), o loader regista `FALHOU` para esse quiz, continua com os restantes e imprime o resumo com o número de falhados; o código de saída é 1. Erros de login ou de listagem abortam logo.

A idempotência é pelo título: os quizzes que já existem (em qualquer estado, incluindo arquivados) são saltados e reportados, por isso uma segunda execução não cria duplicados. Alterar o conteúdo de um quiz já carregado faz-se na administração, não no loader.

## Publicação

```bash
node scripts/load-curated-quizzes.js --publish
```

Publica só os quizzes que já existem em Rascunho ou Retirado e cujo ficheiro tem `provenance.reviewedBy` e `provenance.reviewedAt` preenchidos. Os que não foram revistos são recusados (código de saída diferente de 0), tal como os quizzes Arquivados sem cópia publicável. Se existir no servidor uma cópia Publicada com o mesmo título, o quiz é saltado e nada é publicado. `--publish` não cria quizzes: carrega-os primeiro sem a opção.

Antes de publicar, o loader compara o rascunho guardado no servidor com o ficheiro revisto: dificuldade, número e ordem das perguntas, enunciados, respostas (e a sua ordem), tipo e resposta certa. Os espaços nas pontas não contam, porque o servidor também os remove. Se houver diferenças, por exemplo porque o ficheiro foi corrigido depois de criado o rascunho, ou porque o rascunho foi editado na administração, o quiz é recusado e o loader indica a primeira diferença. Para resolver, atualiza o rascunho na administração para ficar igual ao ficheiro, ou apaga-o e volta a carregar sem `--publish`.

## Retirar a publicação

Na administração, usa a ação "Retirar" do quiz (Publicado para Retirado). O quiz pode ser publicado de novo com `--publish`, desde que o ficheiro continue revisto.

## Testes

```bash
node --test scripts/
```
