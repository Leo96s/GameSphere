'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');

const {
  validateQuiz,
  toUpsertPayload,
  findContentDifference,
  createApiClient,
  run,
} = require('./load-curated-quizzes.js');

function question(overrides = {}) {
  return {
    description: 'Qual é a capital de Espanha?',
    typeOfAnswer: 'MULTIPLEOPTION',
    answers: ['Madrid', 'Barcelona', 'Sevilha', 'Valência'],
    correctAnswer: 'Madrid',
    explanation: 'Madrid é a capital de Espanha.',
    ...overrides,
  };
}

function quiz(overrides = {}) {
  return {
    title: 'Geografia — Capitais da Europa',
    category: 'Geografia',
    difficulty: 'EASY',
    provenance: { source: 'original', author: 'GameSphere', reviewedBy: null, reviewedAt: null },
    questions: [question()],
    ...overrides,
  };
}

function errorsOf(q) {
  return validateQuiz(q, 'x.json');
}

function writeContent(dir, name, value) {
  fs.writeFileSync(path.join(dir, name), JSON.stringify(value), 'utf8');
}

function tmpDir() {
  return fs.mkdtempSync(path.join(os.tmpdir(), 'curated-quizzes-'));
}

const ENV = {
  GAMESPHERE_ADMIN_EMAIL: 'admin@example.test',
  GAMESPHERE_ADMIN_PASSWORD: 'S3cret-Pass-Word!',
};

function fakeClient(existing = []) {
  const state = { quizzes: existing.map((q) => ({ ...q })), created: [], statusChanges: [], nextId: 100 };
  return {
    state,
    login: async () => {},
    listQuizzes: async () => state.quizzes.map((q) => ({ ...q })),
    createQuiz: async (payload) => {
      const created = { ...payload, id: state.nextId++, status: 0 };
      state.created.push(payload);
      state.quizzes.push(created);
      return created;
    },
    changeStatus: async (id, status) => {
      state.statusChanges.push({ id, status });
      const found = state.quizzes.find((q) => q.id === id);
      if (found) found.status = status;
      return found;
    },
  };
}

// Quiz tal como a API de admin o devolve: texto normalizado com Trim pelo servidor,
// perguntas com id e quizzId reais, mais id e estado.
function serverQuiz(id, status, content) {
  const difficulty = { EASY: 0, MEDIUM: 1, HARD: 2 }[content.difficulty];
  const typeOfAnswer = { MULTIPLEOPTION: 0, TRUEORFALSE: 1 };
  return {
    id,
    status,
    title: content.title.trim(),
    difficulty,
    numberOfQuests: content.questions.length,
    questions: content.questions.map((q, i) => ({
      id: id * 1000 + i + 1,
      quizzId: id,
      description: q.description.trim(),
      typeOfAnswer: typeOfAnswer[q.typeOfAnswer],
      answers: q.answers.map((a) => a.trim()),
      correctAnswer: q.correctAnswer.trim(),
    })),
  };
}

function collector() {
  const lines = [];
  return { lines, log: (line) => lines.push(String(line)) };
}

test('validateQuiz aceita um quiz valido', () => {
  assert.deepEqual(errorsOf(quiz()), []);
});

test('validateQuiz: titulo vazio, longo ou com quebra de linha', () => {
  assert.notEqual(errorsOf(quiz({ title: '   ' })).length, 0);
  assert.notEqual(errorsOf(quiz({ title: `Geografia — ${'a'.repeat(100)}` })).length, 0);
  assert.notEqual(errorsOf(quiz({ title: 'Geografia — A\nB' })).length, 0);
  assert.deepEqual(errorsOf(quiz({ title: `Geografia — ${'a'.repeat(100 - 'Geografia — '.length)}` })), []);
});

test('validateQuiz: titulo tem de comecar pela categoria', () => {
  assert.notEqual(errorsOf(quiz({ title: 'Capitais da Europa' })).length, 0);
  assert.notEqual(errorsOf(quiz({ title: 'História — Capitais' })).length, 0);
});

test('validateQuiz: dificuldade invalida', () => {
  assert.notEqual(errorsOf(quiz({ difficulty: 'IMPOSSIBLE' })).length, 0);
  assert.deepEqual(errorsOf(quiz({ difficulty: 'HARD' })), []);
});

test('validateQuiz: 1 a 100 perguntas', () => {
  assert.notEqual(errorsOf(quiz({ questions: [] })).length, 0);
  assert.notEqual(errorsOf(quiz({ questions: Array.from({ length: 101 }, () => question()) })).length, 0);
  assert.deepEqual(errorsOf(quiz({ questions: Array.from({ length: 100 }, () => question()) })), []);
});

test('validateQuiz: descricao da pergunta ate 2000 e nao vazia', () => {
  assert.notEqual(errorsOf(quiz({ questions: [question({ description: ' ' })] })).length, 0);
  assert.notEqual(errorsOf(quiz({ questions: [question({ description: 'a'.repeat(2001) })] })).length, 0);
  assert.deepEqual(errorsOf(quiz({ questions: [question({ description: 'a'.repeat(2000) })] })), []);
  assert.deepEqual(errorsOf(quiz({ questions: [question({ description: 'Linha 1\nLinha 2' })] })), []);
});

test('validateQuiz: 2 a 6 respostas', () => {
  assert.notEqual(errorsOf(quiz({ questions: [question({ answers: ['Madrid'] })] })).length, 0);
  const seven = ['a', 'b', 'c', 'd', 'e', 'f', 'Madrid'];
  assert.notEqual(errorsOf(quiz({ questions: [question({ answers: seven })] })).length, 0);
  const six = ['a', 'b', 'c', 'd', 'e', 'Madrid'];
  assert.deepEqual(errorsOf(quiz({ questions: [question({ answers: six })] })), []);
});

test('validateQuiz: respostas unicas sem distinguir maiusculas', () => {
  const errors = errorsOf(quiz({ questions: [question({ answers: ['Madrid', 'MADRID', 'Sevilha'] })] }));
  assert.notEqual(errors.length, 0);
});

test('validateQuiz: resposta com mais de 500 caracteres ou com quebra de linha', () => {
  assert.notEqual(errorsOf(quiz({ questions: [question({ answers: ['Madrid', 'a'.repeat(501)] })] })).length, 0);
  assert.notEqual(errorsOf(quiz({ questions: [question({ answers: ['Madrid', 'a\nb'] })] })).length, 0);
  assert.notEqual(errorsOf(quiz({ questions: [question({ answers: ['Madrid', ' '] })] })).length, 0);
});

test('validateQuiz: caracteres invisiveis e de controlo proibidos', () => {
  assert.notEqual(errorsOf(quiz({ questions: [question({ answers: ['Madrid', 'Lis​boa'] })] })).length, 0);
  assert.notEqual(errorsOf(quiz({ questions: [question({ description: 'Ola\u0000' })] })).length, 0);
  assert.notEqual(errorsOf(quiz({ title: 'Geografia — A﻿B' })).length, 0);
});

test('validateQuiz: correctAnswer tem de ser uma das respostas (ordinal)', () => {
  assert.notEqual(errorsOf(quiz({ questions: [question({ correctAnswer: 'Lisboa' })] })).length, 0);
  assert.notEqual(errorsOf(quiz({ questions: [question({ correctAnswer: 'madrid' })] })).length, 0);
  assert.deepEqual(errorsOf(quiz({ questions: [question({ correctAnswer: ' Madrid ' })] })), []);
});

test('validateQuiz: TRUEORFALSE com exactamente 2 respostas', () => {
  const tf = (answers, correctAnswer) => question({ typeOfAnswer: 'TRUEORFALSE', answers, correctAnswer });
  assert.deepEqual(errorsOf(quiz({ questions: [tf(['Verdadeiro', 'Falso'], 'Falso')] })), []);
  assert.notEqual(errorsOf(quiz({ questions: [tf(['Verdadeiro', 'Falso', 'Talvez'], 'Falso')] })).length, 0);
  assert.notEqual(errorsOf(quiz({ questions: [question({ typeOfAnswer: 'OUTRO' })] })).length, 0);
});

test('validateQuiz: explanation e provenance sao obrigatorias', () => {
  assert.notEqual(errorsOf(quiz({ questions: [question({ explanation: '' })] })).length, 0);
  assert.notEqual(errorsOf(quiz({ provenance: undefined })).length, 0);
  assert.notEqual(errorsOf(quiz({ provenance: { source: 'copiado', author: 'X', reviewedBy: null, reviewedAt: null } })).length, 0);
});

test('validateQuiz: erros indicam ficheiro e numero da pergunta', () => {
  const errors = validateQuiz(quiz({ questions: [question(), question({ correctAnswer: 'Lisboa' })] }), 'meu.json');
  assert.equal(errors.length, 1);
  assert.match(errors[0], /meu\.json/);
  assert.match(errors[0], /pergunta 2/i);
});

test('toUpsertPayload converte nomes em numeros e descarta campos editoriais', () => {
  const payload = toUpsertPayload(quiz({
    difficulty: 'MEDIUM',
    questions: [
      question(),
      question({ typeOfAnswer: 'TRUEORFALSE', answers: ['Verdadeiro', 'Falso'], correctAnswer: 'Verdadeiro' }),
    ],
  }));
  assert.equal(payload.title, 'Geografia — Capitais da Europa');
  assert.equal(payload.difficulty, 1);
  assert.equal(payload.numberOfQuests, 2);
  assert.equal(payload.questions[0].typeOfAnswer, 0);
  assert.equal(payload.questions[1].typeOfAnswer, 1);
  assert.deepEqual(payload.questions[0].answers, ['Madrid', 'Barcelona', 'Sevilha', 'Valência']);
  assert.equal(payload.questions[0].correctAnswer, 'Madrid');
  assert.equal('category' in payload, false);
  assert.equal('provenance' in payload, false);
  assert.equal('explanation' in payload.questions[0], false);
});

test('dry-run valida sem rede e devolve 0', async () => {
  const dir = tmpDir();
  writeContent(dir, 'a.json', quiz());
  const { log, lines } = collector();
  const client = { login: () => assert.fail('nao deve haver rede'), listQuizzes: () => assert.fail('rede') };
  const code = await run({ args: ['--dry-run'], env: {}, contentDir: dir, client, log });
  assert.equal(code, 0);
  assert.ok(lines.some((l) => l.includes('a.json')));
});

test('dry-run devolve != 0 para ficheiro invalido e indica ficheiro e pergunta', async () => {
  const dir = tmpDir();
  writeContent(dir, 'ok.json', quiz());
  writeContent(dir, 'mau.json', quiz({ questions: [question(), question({ correctAnswer: 'Lisboa' })] }));
  const { log, lines } = collector();
  const code = await run({ args: ['--dry-run'], env: {}, contentDir: dir, log });
  assert.notEqual(code, 0);
  const text = lines.join('\n');
  assert.match(text, /mau\.json/);
  assert.match(text, /pergunta 2/i);
});

test('dry-run: JSON malformado e pasta sem ficheiros falham', async () => {
  const bad = tmpDir();
  fs.writeFileSync(path.join(bad, 'x.json'), '{ nao e json', 'utf8');
  assert.notEqual(await run({ args: ['--dry-run'], env: {}, contentDir: bad, log: () => {} }), 0);
  const empty = tmpDir();
  assert.notEqual(await run({ args: ['--dry-run'], env: {}, contentDir: empty, log: () => {} }), 0);
});

test('criacao: cria rascunhos com perguntas embebidas e segunda execucao nao duplica', async () => {
  const dir = tmpDir();
  writeContent(dir, 'a.json', quiz());
  writeContent(dir, 'b.json', quiz({ title: 'Geografia — Rios' }));
  const client = fakeClient();
  const { log } = collector();
  assert.equal(await run({ args: [], env: ENV, contentDir: dir, client, log }), 0);
  assert.equal(client.state.created.length, 2);
  assert.equal(client.state.created[0].questions.length, 1);
  assert.equal(client.state.statusChanges.length, 0);

  const second = collector();
  assert.equal(await run({ args: [], env: ENV, contentDir: dir, client, log: second.log }), 0);
  assert.equal(client.state.created.length, 2);
  assert.match(second.lines.join('\n'), /saltad/i);
});

test('criacao: salta quizzes existentes em qualquer estado, incluindo arquivados', async () => {
  const dir = tmpDir();
  writeContent(dir, 'a.json', quiz());
  const client = fakeClient([{ id: 1, title: 'Geografia — Capitais da Europa', status: 3 }]);
  assert.equal(await run({ args: [], env: ENV, contentDir: dir, client, log: () => {} }), 0);
  assert.equal(client.state.created.length, 0);
});

test('publish: recusa quizzes sem revisao e devolve != 0', async () => {
  const dir = tmpDir();
  writeContent(dir, 'a.json', quiz());
  const client = fakeClient([serverQuiz(1, 0, quiz())]);
  const { log, lines } = collector();
  const code = await run({ args: ['--publish'], env: ENV, contentDir: dir, client, log });
  assert.notEqual(code, 0);
  assert.equal(client.state.statusChanges.length, 0);
  assert.match(lines.join('\n'), /revis/i);
});

test('publish: publica Draft e Unpublished revistos, salta Published e recusa Archived', async () => {
  const dir = tmpDir();
  const reviewed = { source: 'original', author: 'GameSphere', reviewedBy: 'Ana', reviewedAt: '2026-10-25' };
  writeContent(dir, 'a.json', quiz({ title: 'Geografia — A', provenance: reviewed }));
  writeContent(dir, 'b.json', quiz({ title: 'Geografia — B', provenance: reviewed }));
  writeContent(dir, 'c.json', quiz({ title: 'Geografia — C', provenance: reviewed }));
  writeContent(dir, 'd.json', quiz({ title: 'Geografia — D', provenance: reviewed }));
  const client = fakeClient([
    serverQuiz(1, 0, quiz({ title: 'Geografia — A' })),
    serverQuiz(2, 2, quiz({ title: 'Geografia — B' })),
    serverQuiz(3, 1, quiz({ title: 'Geografia — C' })),
    serverQuiz(4, 3, quiz({ title: 'Geografia — D' })),
  ]);
  const { log, lines } = collector();
  const code = await run({ args: ['--publish'], env: ENV, contentDir: dir, client, log });
  assert.equal(code, 1);
  assert.deepEqual(client.state.statusChanges, [{ id: 1, status: 1 }, { id: 2, status: 1 }]);
  assert.match(lines.join('\n'), /RECUSADO "Geografia — D"/);
  assert.match(lines.join('\n'), /SALTADO "Geografia — C"/);
});

test('publish: Published sozinho e SALTADO com codigo 0', async () => {
  const dir = tmpDir();
  const reviewed = { source: 'original', author: 'GameSphere', reviewedBy: 'Ana', reviewedAt: '2026-10-25' };
  writeContent(dir, 'a.json', quiz({ provenance: reviewed }));
  const client = fakeClient([serverQuiz(1, 1, quiz())]);
  assert.equal(await run({ args: ['--publish'], env: ENV, contentDir: dir, client, log: () => {} }), 0);
  assert.equal(client.state.statusChanges.length, 0);
});

test('publish: recusa quando o conteudo no servidor difere do ficheiro revisto', async () => {
  const reviewed = { source: 'original', author: 'GameSphere', reviewedBy: 'Ana', reviewedAt: '2026-10-25' };
  const cases = [
    ['descricao', { description: 'Qual é a capital de França?' }, /pergunta 1: description/],
    ['respostas', { answers: ['Madrid', 'Barcelona', 'Sevilha', 'Bilbau'] }, /pergunta 1: answers/],
    ['ordem das respostas', { answers: ['Barcelona', 'Madrid', 'Sevilha', 'Valência'] }, /pergunta 1: answers/],
    ['resposta certa', { answers: ['Madrid', 'Barcelona', 'Sevilha', 'Valência'], correctAnswer: 'Sevilha' }, /pergunta 1: correctAnswer/],
    ['tipo', { typeOfAnswer: 'TRUEORFALSE', answers: ['Verdadeiro', 'Falso'], correctAnswer: 'Verdadeiro' }, /pergunta 1: typeOfAnswer/],
  ];
  for (const [label, serverQuestion, expected] of cases) {
    const dir = tmpDir();
    writeContent(dir, 'a.json', quiz({ provenance: reviewed }));
    const client = fakeClient([serverQuiz(1, 0, quiz({ questions: [question(serverQuestion)] }))]);
    const { log, lines } = collector();
    const code = await run({ args: ['--publish'], env: ENV, contentDir: dir, client, log });
    assert.notEqual(code, 0, label);
    assert.equal(client.state.statusChanges.length, 0, label);
    assert.match(lines.join('\n'), /RECUSADO .*difere do ficheiro/, label);
    assert.match(lines.join('\n'), expected, label);
  }
});

test('publish: recusa quando a dificuldade ou o numero de perguntas no servidor difere', async () => {
  const reviewed = { source: 'original', author: 'GameSphere', reviewedBy: 'Ana', reviewedAt: '2026-10-25' };
  const dir = tmpDir();
  writeContent(dir, 'a.json', quiz({ provenance: reviewed }));

  const harder = fakeClient([serverQuiz(1, 0, quiz({ difficulty: 'MEDIUM' }))]);
  const lines1 = collector();
  assert.notEqual(await run({ args: ['--publish'], env: ENV, contentDir: dir, client: harder, log: lines1.log }), 0);
  assert.match(lines1.lines.join('\n'), /difficulty/);

  const longer = fakeClient([serverQuiz(1, 0, quiz({ questions: [question(), question({ description: 'Outra?' })] }))]);
  const lines2 = collector();
  assert.notEqual(await run({ args: ['--publish'], env: ENV, contentDir: dir, client: longer, log: lines2.log }), 0);
  assert.match(lines2.lines.join('\n'), /número de perguntas/);
  assert.equal(harder.state.statusChanges.length + longer.state.statusChanges.length, 0);
});

test('publish: diferencas que o servidor normaliza (espacos nas pontas) nao bloqueiam', async () => {
  const reviewed = { source: 'original', author: 'GameSphere', reviewedBy: 'Ana', reviewedAt: '2026-10-25' };
  const dir = tmpDir();
  writeContent(dir, 'a.json', quiz({
    provenance: reviewed,
    questions: [question({ description: '  Qual é a capital de Espanha?  ', answers: [' Madrid', 'Barcelona ', 'Sevilha', 'Valência'], correctAnswer: ' Madrid ' })],
  }));
  const client = fakeClient([serverQuiz(1, 0, quiz())]);
  const code = await run({ args: ['--publish'], env: ENV, contentDir: dir, client, log: () => {} });
  assert.equal(code, 0);
  assert.deepEqual(client.state.statusChanges, [{ id: 1, status: 1 }]);
});

test('publish: o fluxo criar -> rever -> publicar continua a funcionar com o conteudo criado', async () => {
  const dir = tmpDir();
  writeContent(dir, 'a.json', quiz());
  const client = fakeClient();
  assert.equal(await run({ args: [], env: ENV, contentDir: dir, client, log: () => {} }), 0);
  writeContent(dir, 'a.json', quiz({ provenance: { source: 'original', author: 'GameSphere', reviewedBy: 'Ana', reviewedAt: '2026-10-25' } }));
  assert.equal(await run({ args: ['--publish'], env: ENV, contentDir: dir, client, log: () => {} }), 0);
  assert.deepEqual(client.state.statusChanges, [{ id: 100, status: 1 }]);
});

test('publish: nao cria quizzes em falta', async () => {
  const dir = tmpDir();
  const reviewed = { source: 'original', author: 'GameSphere', reviewedBy: 'Ana', reviewedAt: '2026-10-25' };
  writeContent(dir, 'a.json', quiz({ provenance: reviewed }));
  const client = fakeClient();
  await run({ args: ['--publish'], env: ENV, contentDir: dir, client, log: () => {} });
  assert.equal(client.state.created.length, 0);
});

test('credenciais em falta terminam com erro antes de qualquer rede', async () => {
  const dir = tmpDir();
  writeContent(dir, 'a.json', quiz());
  const { log, lines } = collector();
  const code = await run({ args: [], env: {}, contentDir: dir, log });
  assert.notEqual(code, 0);
  assert.match(lines.join('\n'), /GAMESPHERE_ADMIN_EMAIL/);
});

test('login falhado termina com erro claro sem expor a password', async () => {
  const dir = tmpDir();
  writeContent(dir, 'a.json', quiz());
  const fetchImpl = async () => new Response(JSON.stringify({ message: 'x' }), { status: 401 });
  const client = createApiClient({ baseUrl: 'http://api.test', fetchImpl });
  const { log, lines } = collector();
  const code = await run({ args: [], env: ENV, contentDir: dir, client, log });
  assert.notEqual(code, 0);
  const text = lines.join('\n');
  assert.match(text, /login/i);
  assert.equal(text.includes(ENV.GAMESPHERE_ADMIN_PASSWORD), false);
});

test('createApiClient: login guarda cookies, envia-os e pagina a listagem', async () => {
  const calls = [];
  const page = (start, count) => Array.from({ length: count }, (_, i) => ({ id: start + i, title: `T${start + i}`, status: 0 }));
  const fetchImpl = async (url, init = {}) => {
    calls.push({ url: String(url), init });
    const u = new URL(url);
    if (u.pathname === '/api/User/login') {
      const headers = new Headers();
      headers.append('Set-Cookie', 'auth=abc; path=/; httponly');
      headers.append('Set-Cookie', 'other=def; path=/');
      return new Response('{}', { status: 200, headers });
    }
    if (u.pathname === '/api/admin/quizzes' && (init.method || 'GET') === 'GET') {
      const p = Number(u.searchParams.get('page'));
      return Response.json(p === 1 ? page(1, 50) : page(51, 3));
    }
    if (u.pathname === '/api/admin/quizzes' && init.method === 'POST') {
      return Response.json({ id: 7, title: 'x', status: 0 }, { status: 201 });
    }
    return Response.json({ id: 7, status: 1 });
  };
  const client = createApiClient({ baseUrl: 'http://api.test/', fetchImpl });
  await client.login('a@b.c', 'pw');
  const all = await client.listQuizzes();
  assert.equal(all.length, 53);
  const listCalls = calls.filter((c) => c.url.includes('includeArchived=true'));
  assert.equal(listCalls.length, 2);
  assert.match(listCalls[0].url, /pageSize=50/);
  assert.equal(listCalls[0].init.headers.Cookie, 'auth=abc; other=def');
  assert.equal(calls.some((c) => c.init.headers && 'Origin' in c.init.headers), false);
  const created = await client.createQuiz({ title: 'x' });
  assert.equal(created.id, 7);
  await client.changeStatus(7, 1);
  const last = calls[calls.length - 1];
  assert.match(last.url, /\/api\/admin\/quizzes\/7\/status$/);
  assert.deepEqual(JSON.parse(last.init.body), { status: 1 });
});

// --- Conteudo real (criterios 1, 2, 3 e 8) ---

const REAL_DIR = path.join(__dirname, '..', 'content', 'quizzes');
const realFiles = () => fs.readdirSync(REAL_DIR).filter((n) => n.endsWith('.json')).sort();
const realQuizzes = () => realFiles().map((n) => ({ name: n, quiz: JSON.parse(fs.readFileSync(path.join(REAL_DIR, n), 'utf8')) }));

test('conteudo real (criterio 1): 6 quizzes, 8 perguntas, 3 categorias, EASY e MEDIUM, um so TRUEORFALSE', () => {
  const all = realQuizzes();
  assert.equal(all.length, 6);
  for (const { name, quiz: q } of all) {
    assert.equal(q.questions.length, 8, name);
    assert.ok(q.title.startsWith(`${q.category} — `), name);
  }
  assert.deepEqual([...new Set(all.map((a) => a.quiz.category))].sort(), ['Ciência', 'Geografia', 'História'].sort());
  const difficulties = new Set(all.map((a) => a.quiz.difficulty));
  assert.ok(difficulties.has('EASY') && difficulties.has('MEDIUM'));
  assert.deepEqual([...difficulties].sort(), ['EASY', 'MEDIUM']);
  const onlyTf = all.filter((a) => a.quiz.questions.every((x) => x.typeOfAnswer === 'TRUEORFALSE'));
  assert.equal(onlyTf.length, 1);
  for (const x of onlyTf[0].quiz.questions) assert.deepEqual(x.answers, ['Verdadeiro', 'Falso']);
  assert.equal(new Set(all.map((a) => a.quiz.title)).size, 6);
});

test('conteudo real (criterio 2): todos os ficheiros passam validateQuiz', () => {
  for (const { name, quiz: q } of realQuizzes()) assert.deepEqual(validateQuiz(q, name), [], name);
});

test('conteudo real (criterio 3): explanation e provenance originais, por rever', () => {
  for (const { name, quiz: q } of realQuizzes()) {
    assert.equal(q.provenance.source, 'original', name);
    assert.ok(typeof q.provenance.author === 'string' && q.provenance.author.trim(), name);
    assert.equal(q.provenance.reviewedBy, null, name);
    assert.equal(q.provenance.reviewedAt, null, name);
    q.questions.forEach((x, i) => {
      assert.ok(typeof x.explanation === 'string' && x.explanation.trim().length > 0, `${name} pergunta ${i + 1}`);
    });
  }
});

test('conteudo real (criterio 4): dry-run dos ficheiros reais devolve 0', async () => {
  const { log, lines } = collector();
  assert.equal(await run({ args: ['--dry-run'], env: {}, log }), 0);
  assert.equal(lines.filter((l) => l.startsWith('OK ')).length, 6);
});

test('criterio 8: README de conteudo documenta os 5 temas e README raiz liga-lhe', () => {
  const doc = fs.readFileSync(path.join(REAL_DIR, 'README.md'), 'utf8');
  for (const re of [/formato/i, /revis/i, /carregamento/i, /publica/i, /retirar a publica/i]) assert.match(doc, re);
  assert.match(doc, /--dry-run/);
  assert.match(doc, /--publish/);
  const root = fs.readFileSync(path.join(__dirname, '..', 'README.md'), 'utf8');
  assert.match(root, /\(content\/quizzes\/README\.md\)/);
});

// --- Gaps de criterios 2, 5, 6 e 7 ---

test('validateQuiz: descricao de pergunta com uma quebra de linha e permitida', () => {
  assert.deepEqual(errorsOf(quiz({ questions: [question({ description: 'Linha 1\nLinha 2' })] })), []);
});

test('publish: reviewedBy so com espacos conta como nao revisto', async () => {
  const dir = tmpDir();
  writeContent(dir, 'a.json', quiz({ provenance: { source: 'original', author: 'G', reviewedBy: '   ', reviewedAt: null } }));
  const client = fakeClient([serverQuiz(1, 0, quiz())]);
  assert.notEqual(await run({ args: ['--publish'], env: ENV, contentDir: dir, client, log: () => {} }), 0);
  assert.equal(client.state.statusChanges.length, 0);
});

test('publish: revisto e publicado, mas um nao revisto no mesmo lote faz falhar sem bloquear o revisto', async () => {
  const dir = tmpDir();
  const reviewed = { source: 'original', author: 'G', reviewedBy: 'Ana', reviewedAt: '2026-10-25' };
  writeContent(dir, 'a.json', quiz({ title: 'Geografia — A', provenance: reviewed }));
  writeContent(dir, 'b.json', quiz({ title: 'Geografia — B' }));
  const client = fakeClient([
    serverQuiz(1, 0, quiz({ title: 'Geografia — A' })),
    serverQuiz(2, 0, quiz({ title: 'Geografia — B' })),
  ]);
  assert.notEqual(await run({ args: ['--publish'], env: ENV, contentDir: dir, client, log: () => {} }), 0);
  assert.deepEqual(client.state.statusChanges, [{ id: 1, status: 1 }]);
});

test('criacao: payload enviado e valido para rascunho (sem status, sem campos editoriais)', async () => {
  const dir = tmpDir();
  writeContent(dir, 'a.json', quiz());
  const client = fakeClient();
  await run({ args: [], env: ENV, contentDir: dir, client, log: () => {} });
  const p = client.state.created[0];
  assert.equal('status' in p, false);
  assert.equal('explanation' in p.questions[0], false);
  assert.equal(p.numberOfQuests, p.questions.length);
});

test('criacao: listagem pagina para detectar duplicados alem da primeira pagina', async () => {
  const dir = tmpDir();
  writeContent(dir, 'a.json', quiz({ title: 'Geografia — Tardio' }));
  const fetchImpl = async (url, init = {}) => {
    const u = new URL(url);
    if (u.pathname === '/api/User/login') {
      const h = new Headers(); h.append('Set-Cookie', 'auth=1; path=/');
      return new Response('{}', { status: 200, headers: h });
    }
    if ((init.method || 'GET') === 'GET') {
      const p = Number(u.searchParams.get('page'));
      return Response.json(p === 1
        ? Array.from({ length: 50 }, (_, i) => ({ id: i + 1, title: `T${i}`, status: 0 }))
        : [{ id: 99, title: 'Geografia — Tardio', status: 3 }]);
    }
    assert.fail('nao deve criar');
  };
  const client = createApiClient({ baseUrl: 'http://api.test', fetchImpl });
  assert.equal(await run({ args: [], env: ENV, contentDir: dir, client, log: () => {} }), 0);
});

test('credenciais: URL por omissao e http://localhost:8080 e GAMESPHERE_BASE_URL e respeitado', async () => {
  const dir = tmpDir();
  writeContent(dir, 'a.json', quiz());
  const original = globalThis.fetch;
  const seen = [];
  globalThis.fetch = async (url) => { seen.push(String(url)); return new Response('{}', { status: 401 }); };
  try {
    await run({ args: [], env: ENV, contentDir: dir, log: () => {} });
    await run({ args: [], env: { ...ENV, GAMESPHERE_BASE_URL: 'https://outro.test:9000/' }, contentDir: dir, log: () => {} });
  } finally {
    globalThis.fetch = original;
  }
  assert.equal(seen[0], 'http://localhost:8080/api/User/login');
  assert.equal(seen[1], 'https://outro.test:9000/api/User/login');
});

test('credenciais: password so e enviada no corpo do login, nunca registada (sucesso e falha)', async () => {
  const dir = tmpDir();
  writeContent(dir, 'a.json', quiz());
  const bodies = [];
  const fetchImpl = async (url, init = {}) => {
    const u = new URL(url);
    if (u.pathname === '/api/User/login') {
      bodies.push(init.body);
      const h = new Headers(); h.append('Set-Cookie', 'auth=1; path=/');
      return new Response('{}', { status: 200, headers: h });
    }
    if ((init.method || 'GET') === 'GET') return Response.json([]);
    return new Response(`erro ${ENV.GAMESPHERE_ADMIN_PASSWORD}`.slice(0, 5), { status: 500 });
  };
  const client = createApiClient({ baseUrl: 'http://api.test', fetchImpl });
  const { log, lines } = collector();
  assert.notEqual(await run({ args: [], env: ENV, contentDir: dir, client, log }), 0);
  assert.equal(lines.join('\n').includes(ENV.GAMESPHERE_ADMIN_PASSWORD), false);
  assert.equal(lines.join('\n').includes(ENV.GAMESPHERE_ADMIN_EMAIL), false);
  assert.equal(bodies.length, 1);

  const ok = collector();
  const okClient = fakeClient();
  assert.equal(await run({ args: [], env: ENV, contentDir: dir, client: okClient, log: ok.log }), 0);
  assert.equal(ok.lines.join('\n').includes(ENV.GAMESPHERE_ADMIN_PASSWORD), false);
});

test('login: resposta 200 sem cookies e tratada como falha', async () => {
  const client = createApiClient({ baseUrl: 'http://api.test', fetchImpl: async () => new Response('{}', { status: 200 }) });
  await assert.rejects(() => client.login('a@b.c', 'pw'), /login/i);
});

// --- Ronda de correccao: paridade com o backend, redirects, duplicados ---

test('validateQuiz: quebra de linha no texto bruto de resposta e correctAnswer falha (backend valida antes do Trim)', () => {
  assert.notEqual(errorsOf(quiz({ questions: [question({ answers: ['Madrid\n', 'Barcelona'] })] })).length, 0);
  assert.notEqual(errorsOf(quiz({ questions: [question({ correctAnswer: 'Madrid\n' })] })).length, 0);
});

test('validateQuiz: titulo com U+FEFF inicial falha (Trim do .NET nao o remove)', () => {
  assert.notEqual(errorsOf(quiz({ title: '﻿Geografia — Capitais da Europa' })).length, 0);
  assert.notEqual(errorsOf(quiz({ title: 'Geografia — Capitais﻿' })).length, 0);
  assert.deepEqual(errorsOf(quiz({ title: '  Geografia — Capitais da Europa ' })), []);
});

test('validateQuiz: unicidade de respostas equivale a OrdinalIgnoreCase', () => {
  const withAnswers = (answers) => quiz({ questions: [question({ answers, correctAnswer: answers[0] })] });
  assert.deepEqual(errorsOf(withAnswers(['Straße', 'STRASSE'])), []);
  assert.notEqual(errorsOf(withAnswers(['Lisboa', 'LISBOA'])).length, 0);
});

test('createApiClient: redirect 307 no login nao e seguido e nao expoe credenciais', async () => {
  const seen = [];
  const fetchImpl = async (url, init = {}) => {
    seen.push({ url: String(url), redirect: init.redirect });
    return new Response(null, { status: 307, headers: { Location: 'http://evil.test/' } });
  };
  const client = createApiClient({ baseUrl: 'http://api.test', fetchImpl });
  await assert.rejects(() => client.login('a@b.c', 'S3cret'), (error) => {
    assert.match(error.message, /307/);
    assert.equal(error.message.includes('S3cret'), false);
    assert.equal(error.message.includes('a@b.c'), false);
    return true;
  });
  assert.equal(seen.length, 1);
  assert.equal(seen[0].redirect, 'manual');
});

test('dry-run: titulos duplicados entre ficheiros falham e indicam os ficheiros', async () => {
  const dir = tmpDir();
  writeContent(dir, 'a.json', quiz());
  writeContent(dir, 'b.json', quiz());
  const { log, lines } = collector();
  assert.notEqual(await run({ args: ['--dry-run'], env: {}, contentDir: dir, log }), 0);
  const text = lines.join('\n');
  assert.match(text, /duplicado/i);
  assert.match(text, /a\.json/);
  assert.match(text, /b\.json/);
});

test('publish: com copia Published no servidor salta e nao publica nenhuma copia Draft/Unpublished', async () => {
  const dir = tmpDir();
  const reviewed = { source: 'original', author: 'G', reviewedBy: 'Ana', reviewedAt: '2026-10-25' };
  writeContent(dir, 'a.json', quiz({ provenance: reviewed }));
  const client = fakeClient([
    serverQuiz(1, 3, quiz()),
    serverQuiz(2, 0, quiz()),
    serverQuiz(5, 2, quiz()),
    serverQuiz(7, 1, quiz()),
  ]);
  const { log, lines } = collector();
  assert.equal(await run({ args: ['--publish'], env: ENV, contentDir: dir, client, log }), 0);
  assert.deepEqual(client.state.statusChanges, []);
  assert.match(lines.join('\n'), /SALTADO .*já está publicado \(id 7\)/);
});

test('publish: sem copia Published escolhe Draft/Unpublished de maior id', async () => {
  const dir = tmpDir();
  const reviewed = { source: 'original', author: 'G', reviewedBy: 'Ana', reviewedAt: '2026-10-25' };
  writeContent(dir, 'a.json', quiz({ provenance: reviewed }));
  const client = fakeClient([
    serverQuiz(1, 3, quiz()),
    serverQuiz(2, 0, quiz()),
    serverQuiz(5, 2, quiz()),
  ]);
  assert.equal(await run({ args: ['--publish'], env: ENV, contentDir: dir, client, log: () => {} }), 0);
  assert.deepEqual(client.state.statusChanges, [{ id: 5, status: 1 }]);
});

test('logs mostram o estado pelo nome', async () => {
  const dir = tmpDir();
  writeContent(dir, 'a.json', quiz());
  const client = fakeClient([{ id: 1, title: 'Geografia — Capitais da Europa', status: 3 }]);
  const { log, lines } = collector();
  await run({ args: [], env: ENV, contentDir: dir, client, log });
  assert.match(lines.join('\n'), /Archived/);

  const reviewed = { source: 'original', author: 'G', reviewedBy: 'Ana', reviewedAt: '2026-10-25' };
  writeContent(dir, 'a.json', quiz({ provenance: reviewed }));
  const pub = collector();
  await run({ args: ['--publish'], env: ENV, contentDir: dir, client, log: pub.log });
  assert.match(pub.lines.join('\n'), /Archived/);
});

// --- Ronda de correccao 2: tamanhos, URL base, listagem, falhas parciais, revisao, diferencas ---

test('validateQuiz: tamanhos medem-se depois do Trim (descricao 1999 + espacos) e o payload vai aparado', () => {
  const q = quiz({ questions: [question({ description: `${'a'.repeat(1999)}${' '.repeat(10)}` })] });
  assert.deepEqual(errorsOf(q), []);
  assert.equal(toUpsertPayload(q).questions[0].description.length, 1999);
});

test('validateQuiz: correctAnswer com espacos a mais vai aparado no payload', () => {
  const q = quiz({ questions: [question({
    answers: ['A', 'B'],
    correctAnswer: `A${' '.repeat(600)}`,
  })] });
  assert.deepEqual(errorsOf(q), []);
  assert.equal(toUpsertPayload(q).questions[0].correctAnswer, 'A');
});

test('toUpsertPayload: respostas vao aparadas e Madrid com quebra de linha continua rejeitado', () => {
  const q = quiz({ questions: [question({ answers: [' Madrid ', ' Barcelona'], correctAnswer: ' Madrid ' })] });
  assert.deepEqual(toUpsertPayload(q).questions[0].answers, ['Madrid', 'Barcelona']);
  assert.notEqual(errorsOf(quiz({ questions: [question({ correctAnswer: 'Madrid\n' })] })).length, 0);
});

async function runWithUrl(baseUrl) {
  const dir = tmpDir();
  writeContent(dir, 'a.json', quiz());
  const original = globalThis.fetch;
  let calls = 0;
  globalThis.fetch = async () => { calls += 1; return new Response('{}', { status: 401 }); };
  const { log, lines } = collector();
  let code;
  try {
    code = await run({ args: [], env: { ...ENV, GAMESPHERE_BASE_URL: baseUrl }, contentDir: dir, log });
  } finally {
    globalThis.fetch = original;
  }
  return { code, calls, text: lines.join('\n') };
}

test('URL base: invalida, protocolo nao http(s), credenciais e http remoto sao recusados sem rede', async () => {
  for (const url of ['nao e url', 'ftp://api.test', 'file:///etc/passwd', 'https://user:Segredo123@api.test', 'https://user@api.test', 'http://api.test', 'http://10.0.0.5:8080', 'http://localhost.evil.test']) {
    const { code, calls, text } = await runWithUrl(url);
    assert.equal(code, 1, url);
    assert.equal(calls, 0, url);
    assert.match(text, /GAMESPHERE_BASE_URL/, url);
    assert.equal(text.includes('Segredo123'), false, url);
    assert.equal(text.includes('user:'), false, url);
    assert.equal(text.includes(url), false, url);
  }
});

test('URL base: https remoto e http em loopback sao aceites', async () => {
  for (const url of ['https://api.test', 'http://localhost:8080', 'http://127.0.0.1:5000', 'http://127.1.2.3', 'http://[::1]:8080']) {
    const { calls, text } = await runWithUrl(url);
    assert.equal(calls, 1, url);
    assert.doesNotMatch(text, /GAMESPHERE_BASE_URL.*(inválid|https)/, url);
  }
});

function listFetch(handler) {
  return async (url, init = {}) => {
    const u = new URL(url);
    if (u.pathname === '/api/User/login') {
      const h = new Headers(); h.append('Set-Cookie', 'auth=1; path=/');
      return new Response('{}', { status: 200, headers: h });
    }
    return Response.json(handler(Number(u.searchParams.get('page')), init));
  };
}

test('listQuizzes: resposta que nao e array falha com erro claro', async () => {
  const client = createApiClient({ baseUrl: 'http://api.test', fetchImpl: listFetch(() => ({ items: [] })) });
  await client.login('a@b.c', 'pw');
  await assert.rejects(() => client.listQuizzes(), /Listagem de quizzes: resposta inesperada/);
});

test('listQuizzes: pagina repetida falha em vez de ciclo infinito', async () => {
  const full = Array.from({ length: 50 }, (_, i) => ({ id: i + 1, title: `T${i}`, status: 0 }));
  const client = createApiClient({ baseUrl: 'http://api.test', fetchImpl: listFetch(() => full) });
  await client.login('a@b.c', 'pw');
  await assert.rejects(() => client.listQuizzes(), /Listagem de quizzes: página 2 repetida/);
});

test('listQuizzes: limite de 10000 paginas falha com erro claro', async () => {
  let pages = 0;
  const fetchImpl = listFetch((page) => {
    pages += 1;
    return Array.from({ length: 50 }, (_, i) => ({ id: page * 100 + i, title: 'T', status: 0 }));
  });
  const client = createApiClient({ baseUrl: 'http://api.test', fetchImpl });
  await client.login('a@b.c', 'pw');
  await assert.rejects(() => client.listQuizzes(), /Listagem de quizzes: .*10000/);
  assert.equal(pages, 10000);
});

test('publish: falha HTTP a meio do lote nao impede os restantes, imprime resumo e devolve 1', async () => {
  const dir = tmpDir();
  const reviewed = { source: 'original', author: 'G', reviewedBy: 'Ana', reviewedAt: '2026-10-25' };
  const titles = ['Geografia — A', 'Geografia — B', 'Geografia — C'];
  titles.forEach((title, i) => writeContent(dir, `${i}.json`, quiz({ title, provenance: reviewed })));
  const client = fakeClient(titles.map((title, i) => serverQuiz(i + 1, 0, quiz({ title }))));
  const original = client.changeStatus;
  client.changeStatus = async (id, status) => {
    if (id === 2) throw new Error('Mudança de estado falhou (HTTP 409)');
    return original(id, status);
  };
  const { log, lines } = collector();
  assert.equal(await run({ args: ['--publish'], env: ENV, contentDir: dir, client, log }), 1);
  assert.deepEqual(client.state.statusChanges, [{ id: 1, status: 1 }, { id: 3, status: 1 }]);
  const text = lines.join('\n');
  assert.match(text, /FALHOU "Geografia — B": Mudança de estado falhou \(HTTP 409\)/);
  assert.match(text, /Publicados: 2, recusados: 0, saltados: 0, falhados: 1\./);
});

test('criacao: falha HTTP a meio do lote nao impede os restantes, imprime resumo e devolve 1', async () => {
  const dir = tmpDir();
  ['A', 'B', 'C'].forEach((n, i) => writeContent(dir, `${i}.json`, quiz({ title: `Geografia — ${n}` })));
  const client = fakeClient();
  const original = client.createQuiz;
  client.createQuiz = async (payload) => {
    if (payload.title === 'Geografia — B') throw new Error('Criação do quiz falhou (HTTP 409)');
    return original(payload);
  };
  const { log, lines } = collector();
  assert.equal(await run({ args: [], env: ENV, contentDir: dir, client, log }), 1);
  assert.equal(client.state.created.length, 2);
  const text = lines.join('\n');
  assert.match(text, /FALHOU "Geografia — B": Criação do quiz falhou \(HTTP 409\)/);
  assert.match(text, /Criados: 2, saltados: 0, falhados: 1\./);
});

test('erro de listagem continua a abortar o lote', async () => {
  const dir = tmpDir();
  writeContent(dir, 'a.json', quiz());
  const client = fakeClient();
  client.listQuizzes = async () => { throw new Error('Listagem de quizzes falhou (HTTP 500)'); };
  const { log, lines } = collector();
  assert.equal(await run({ args: [], env: ENV, contentDir: dir, client, log }), 1);
  assert.match(lines.join('\n'), /Listagem de quizzes falhou/);
  assert.equal(client.state.created.length, 0);
});

test('validateQuiz: reviewedBy e reviewedAt ambos null ou ambos preenchidos, data real AAAA-MM-DD', () => {
  const prov = (reviewedBy, reviewedAt) => quiz({ provenance: { source: 'original', author: 'G', reviewedBy, reviewedAt } });
  assert.deepEqual(errorsOf(prov(null, null)), []);
  assert.deepEqual(errorsOf(prov('Ana', '2026-10-25')), []);
  assert.notEqual(errorsOf(prov('Ana', null)).length, 0);
  assert.notEqual(errorsOf(prov(null, '2026-10-25')).length, 0);
  assert.notEqual(errorsOf(prov('Ana', '2026-02-30')).length, 0);
  assert.notEqual(errorsOf(prov('Ana', '25/10/2026')).length, 0);
  assert.notEqual(errorsOf(prov('Ana', 'ontem')).length, 0);
});

test('publish: revisao incompleta (so reviewedBy) nao publica e devolve 1', async () => {
  const dir = tmpDir();
  writeContent(dir, 'a.json', quiz({ provenance: { source: 'original', author: 'G', reviewedBy: 'Ana', reviewedAt: null } }));
  const client = fakeClient([serverQuiz(1, 0, quiz())]);
  assert.equal(await run({ args: ['--publish'], env: ENV, contentDir: dir, client, log: () => {} }), 1);
  assert.equal(client.state.statusChanges.length, 0);
});

test('findContentDifference: igual devolve null e cada diferenca e detectada', () => {
  const base = quiz();
  const same = serverQuiz(1, 0, base);
  assert.equal(findContentDifference(base, same), null);

  const variant = (overrides) => serverQuiz(1, 0, quiz(overrides));
  assert.match(findContentDifference(base, variant({ difficulty: 'HARD' })), /difficulty/);
  assert.match(findContentDifference(base, variant({ questions: [question(), question()] })), /número de perguntas/);
  assert.match(findContentDifference(base, variant({ questions: [question({ description: 'Outra?' })] })), /pergunta 1: description/);
  assert.match(findContentDifference(base, variant({ questions: [question({ typeOfAnswer: 'TRUEORFALSE', answers: ['Verdadeiro', 'Falso'], correctAnswer: 'Verdadeiro' })] })), /pergunta 1: typeOfAnswer/);
  assert.match(findContentDifference(base, variant({ questions: [question({ answers: ['Madrid', 'Barcelona', 'Sevilha', 'Bilbau'] })] })), /pergunta 1: answers/);
  assert.match(findContentDifference(base, variant({ questions: [question({ answers: ['Barcelona', 'Madrid', 'Sevilha', 'Valência'] })] })), /pergunta 1: answers/);
  assert.match(findContentDifference(base, variant({ questions: [question({ correctAnswer: 'Sevilha' })] })), /pergunta 1: correctAnswer/);
});

test('findContentDifference: espacos nas pontas no ficheiro vs texto normalizado do servidor dao null', () => {
  const padded = quiz({
    title: ' Geografia — Capitais da Europa ',
    questions: [question({ description: '  Qual é a capital de Espanha?  ', answers: [' Madrid', 'Barcelona ', 'Sevilha', ' Valência '], correctAnswer: ' Madrid ' })],
  });
  assert.equal(findContentDifference(padded, serverQuiz(1, 0, quiz())), null);
});
