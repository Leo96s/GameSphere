#!/usr/bin/env node
'use strict';

const fs = require('node:fs');
const path = require('node:path');

const DEFAULT_CONTENT_DIR = path.join(__dirname, '..', 'content', 'quizzes');
const DEFAULT_BASE_URL = 'http://localhost:8080';
const PAGE_SIZE = 50;
const MAX_PAGES = 10000; // alinhado com o clamp de page em GetQuizzesHandler.cs

const DIFFICULTY = { EASY: 0, MEDIUM: 1, HARD: 2 };
const TYPE_OF_ANSWER = { MULTIPLEOPTION: 0, TRUEORFALSE: 1 };
const STATUS = { Draft: 0, Published: 1, Unpublished: 2, Archived: 3 };

const MAX_TITLE = 100;
const MAX_QUESTIONS = 100;
const MAX_DESCRIPTION = 2000;
const MIN_ANSWERS = 2;
const MAX_ANSWERS = 6;
const MAX_ANSWER = 500;

// Espelha EditorialTextValidator.cs: controlo (Cc) e caracteres invisiveis.
const FORBIDDEN_CHARS = /[\p{Cc}​‌‎‏﻿‪-‮⁠-⁤⁦-⁩]/u;

function hasForbiddenChars(text, allowLineBreaks) {
  const candidate = allowLineBreaks ? text.replace(/[\t\n\r]/g, '') : text;
  return FORBIDDEN_CHARS.test(candidate);
}

// string.Trim() do .NET: whitespace Unicode sem U+FEFF (ao contrario do trim do JS).
const DOTNET_WS = '\\t-\\r \\u0085\\u00A0\\u1680\\u2000-\\u200A\\u2028\\u2029\\u202F\\u205F\\u3000';
const DOTNET_TRIM = new RegExp(`^[${DOTNET_WS}]+|[${DOTNET_WS}]+$`, 'gu');

function dotnetTrim(text) {
  return text.replace(DOTNET_TRIM, '');
}

// Equivalente a StringComparer.OrdinalIgnoreCase: maiusculas por code point, sem expansoes (ß->SS).
function ordinalUpper(text) {
  let result = '';
  for (const codePoint of text) {
    const upper = codePoint.toUpperCase();
    result += [...upper].length === 1 ? upper : codePoint;
  }
  return result;
}

function isNonEmptyString(value) {
  return typeof value === 'string' && dotnetTrim(value).length > 0;
}

function isRealDate(value) {
  if (typeof value !== 'string' || !/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
  const date = new Date(`${value}T00:00:00Z`);
  return !Number.isNaN(date.getTime()) && date.toISOString().slice(0, 10) === value;
}

function validateQuestion(question, label) {
  const errors = [];
  const fail = (message) => errors.push(`${label}: ${message}`);

  if (question === null || typeof question !== 'object') {
    fail('a pergunta não é um objeto');
    return errors;
  }

  if (!isNonEmptyString(question.description)) {
    fail('descrição em falta');
  } else {
    if (dotnetTrim(question.description).length > MAX_DESCRIPTION) {
      fail(`descrição com mais de ${MAX_DESCRIPTION} caracteres`);
    }
    if (hasForbiddenChars(question.description, true)) {
      fail('descrição com caracteres de controlo ou invisíveis');
    }
  }

  if (!Object.hasOwn(TYPE_OF_ANSWER, question.typeOfAnswer)) {
    fail(`typeOfAnswer inválido (${JSON.stringify(question.typeOfAnswer)})`);
  }

  if (!isNonEmptyString(question.explanation)) {
    fail('explanation em falta');
  }

  if (!Array.isArray(question.answers)) {
    fail('answers tem de ser uma lista');
    return errors;
  }

  if (question.answers.length < MIN_ANSWERS || question.answers.length > MAX_ANSWERS) {
    fail(`tem de ter entre ${MIN_ANSWERS} e ${MAX_ANSWERS} respostas`);
  }

  const answers = [];
  for (const answer of question.answers) {
    if (typeof answer !== 'string' || dotnetTrim(answer).length === 0) {
      fail('resposta vazia ou que não é texto');
      return errors;
    }
    const trimmed = dotnetTrim(answer);
    if (trimmed.length > MAX_ANSWER) {
      fail(`resposta com mais de ${MAX_ANSWER} caracteres`);
    }
    // O backend valida o texto bruto, antes do Trim.
    if (hasForbiddenChars(answer, false)) {
      fail(`resposta "${trimmed.slice(0, 40)}" com quebras de linha, caracteres de controlo ou invisíveis`);
    }
    answers.push(trimmed);
  }

  if (new Set(answers.map(ordinalUpper)).size !== answers.length) {
    fail('respostas repetidas (sem distinguir maiúsculas)');
  }

  if (!isNonEmptyString(question.correctAnswer)) {
    fail('correctAnswer em falta');
  } else {
    const correct = dotnetTrim(question.correctAnswer);
    if (hasForbiddenChars(question.correctAnswer, false)) {
      fail('correctAnswer com caracteres proibidos');
    }
    if (!answers.includes(correct)) {
      fail('correctAnswer não é igual a nenhuma das respostas');
    }
  }

  if (question.typeOfAnswer === 'TRUEORFALSE' && answers.length !== 2) {
    fail('TRUEORFALSE tem de ter exatamente 2 respostas');
  }

  return errors;
}

function validateQuiz(quiz, fileName) {
  const errors = [];
  const fail = (message) => errors.push(`${fileName}: ${message}`);

  if (quiz === null || typeof quiz !== 'object' || Array.isArray(quiz)) {
    fail('o conteúdo não é um objeto');
    return errors;
  }

  if (!isNonEmptyString(quiz.title)) {
    fail('título em falta');
  } else {
    const title = dotnetTrim(quiz.title);
    if (title.length > MAX_TITLE) {
      fail(`título com mais de ${MAX_TITLE} caracteres`);
    }
    if (hasForbiddenChars(title, false)) {
      fail('título com quebras de linha, caracteres de controlo ou invisíveis');
    }
    if (!isNonEmptyString(quiz.category)) {
      fail('category em falta');
    } else if (!title.startsWith(`${quiz.category} — `)) {
      fail(`o título tem de começar por "${quiz.category} — "`);
    }
  }

  if (!Object.hasOwn(DIFFICULTY, quiz.difficulty)) {
    fail(`difficulty inválida (${JSON.stringify(quiz.difficulty)})`);
  }

  const provenance = quiz.provenance;
  if (provenance === null || typeof provenance !== 'object') {
    fail('provenance em falta');
  } else {
    if (provenance.source !== 'original') fail('provenance.source tem de ser "original"');
    if (!isNonEmptyString(provenance.author)) fail('provenance.author em falta');
    if (provenance.reviewedBy !== null && !isNonEmptyString(provenance.reviewedBy)) {
      fail('provenance.reviewedBy tem de ser null ou texto');
    }
    if (provenance.reviewedAt !== null && !isRealDate(provenance.reviewedAt)) {
      fail('provenance.reviewedAt tem de ser null ou uma data real AAAA-MM-DD');
    }
    if ((provenance.reviewedBy === null) !== (provenance.reviewedAt === null)) {
      fail('provenance.reviewedBy e provenance.reviewedAt têm de ser ambos null ou ambos preenchidos');
    }
  }

  if (!Array.isArray(quiz.questions) || quiz.questions.length < 1 || quiz.questions.length > MAX_QUESTIONS) {
    fail(`tem de ter entre 1 e ${MAX_QUESTIONS} perguntas`);
    return errors;
  }

  quiz.questions.forEach((question, index) => {
    errors.push(...validateQuestion(question, `${fileName}: pergunta ${index + 1}`));
  });

  return errors;
}

function toUpsertPayload(quiz) {
  return {
    title: dotnetTrim(quiz.title),
    difficulty: DIFFICULTY[quiz.difficulty],
    numberOfQuests: quiz.questions.length,
    questions: quiz.questions.map((question) => ({
      // O backend valida os tamanhos no texto bruto, por isso o texto já vai aparado.
      description: dotnetTrim(question.description),
      typeOfAnswer: TYPE_OF_ANSWER[question.typeOfAnswer],
      answers: question.answers.map(dotnetTrim),
      correctAnswer: dotnetTrim(question.correctAnswer),
      quizzId: 0,
    })),
  };
}

function createApiClient({ baseUrl, fetchImpl = globalThis.fetch }) {
  const root = String(baseUrl).replace(/\/+$/, '');
  let cookieHeader = '';

  async function request(method, pathname, body) {
    const headers = { Accept: 'application/json' };
    if (body !== undefined) headers['Content-Type'] = 'application/json';
    if (cookieHeader) headers.Cookie = cookieHeader;
    const response = await fetchImpl(`${root}${pathname}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
      redirect: 'manual',
    });
    if (response.status >= 300 && response.status < 400) {
      throw new Error(`Redirecionamento inesperado (HTTP ${response.status}) em ${method} ${pathname}; não é seguido. Verifica GAMESPHERE_BASE_URL.`);
    }
    return response;
  }

  async function expectJson(response, label) {
    if (!response.ok) {
      let detail = '';
      try {
        const text = await response.text();
        detail = text.slice(0, 300);
      } catch {
        detail = '';
      }
      throw new Error(`${label} falhou (HTTP ${response.status})${detail ? `: ${detail}` : ''}`);
    }
    return response.json();
  }

  return {
    async login(email, password) {
      const response = await request('POST', '/api/User/login', { email, password });
      if (!response.ok) {
        throw new Error(`Login falhou (HTTP ${response.status}). Verifica GAMESPHERE_ADMIN_EMAIL, GAMESPHERE_ADMIN_PASSWORD e GAMESPHERE_BASE_URL.`);
      }
      const setCookies = typeof response.headers.getSetCookie === 'function'
        ? response.headers.getSetCookie()
        : [];
      cookieHeader = setCookies.map((cookie) => cookie.split(';')[0].trim()).filter(Boolean).join('; ');
      if (!cookieHeader) {
        throw new Error('Login falhou: a resposta não definiu cookies de sessão.');
      }
    },

    async listQuizzes() {
      const all = [];
      let previousIds = null;
      for (let page = 1; page <= MAX_PAGES; page += 1) {
        const response = await request(
          'GET',
          `/api/admin/quizzes?includeArchived=true&page=${page}&pageSize=${PAGE_SIZE}`,
        );
        const items = await expectJson(response, 'Listagem de quizzes');
        if (!Array.isArray(items)) {
          throw new Error('Listagem de quizzes: resposta inesperada');
        }
        const ids = items.map((item) => item && item.id).join(',');
        if (items.length > 0 && ids === previousIds) {
          throw new Error(`Listagem de quizzes: página ${page} repetida (mesmos ids que a anterior)`);
        }
        previousIds = ids;
        all.push(...items);
        if (items.length < PAGE_SIZE) return all;
      }
      throw new Error(`Listagem de quizzes: excedeu o limite de ${MAX_PAGES} páginas`);
    },

    async createQuiz(payload) {
      return expectJson(await request('POST', '/api/admin/quizzes', payload), 'Criação do quiz');
    },

    async changeStatus(id, status) {
      return expectJson(
        await request('POST', `/api/admin/quizzes/${id}/status`, { status }),
        'Mudança de estado',
      );
    },
  };
}

// Compara o ficheiro revisto com o quiz guardado no servidor, depois de aplicar a mesma
// normalização que o backend (trim .NET de título, descrição, respostas e resposta certa).
// Devolve a primeira diferença encontrada, ou null se o conteúdo for igual.
function findContentDifference(quiz, serverQuiz) {
  const local = toUpsertPayload(quiz);
  if (local.difficulty !== serverQuiz.difficulty) return 'difficulty';

  const localQuestions = local.questions;
  const serverQuestions = Array.isArray(serverQuiz.questions) ? serverQuiz.questions : [];
  if (localQuestions.length !== serverQuestions.length) {
    return `número de perguntas (${localQuestions.length} no ficheiro, ${serverQuestions.length} no servidor)`;
  }

  const sameText = (a, b) => dotnetTrim(String(a ?? '')) === dotnetTrim(String(b ?? ''));
  for (let i = 0; i < localQuestions.length; i += 1) {
    const mine = localQuestions[i];
    const theirs = serverQuestions[i];
    const label = `pergunta ${i + 1}`;
    if (!sameText(mine.description, theirs.description)) return `${label}: description`;
    if (mine.typeOfAnswer !== theirs.typeOfAnswer) return `${label}: typeOfAnswer`;
    const theirAnswers = Array.isArray(theirs.answers) ? theirs.answers : [];
    if (mine.answers.length !== theirAnswers.length
      || mine.answers.some((answer, j) => !sameText(answer, theirAnswers[j]))) {
      return `${label}: answers`;
    }
    if (!sameText(mine.correctAnswer, theirs.correctAnswer)) return `${label}: correctAnswer`;
  }
  return null;
}

function statusName(status) {
  const name = Object.keys(STATUS).find((key) => STATUS[key] === status);
  return name || String(status);
}

function pickByTitle(items, publishing) {
  const groups = new Map();
  for (const item of items) {
    const key = dotnetTrim(String(item.title));
    groups.set(key, [...(groups.get(key) || []), item]);
  }
  const byTitle = new Map();
  for (const [key, group] of groups) {
    if (!publishing) {
      byTitle.set(key, group[0]);
      continue;
    }
    // Com publicação: uma cópia Published ganha sempre; senão o Draft/Unpublished de maior id.
    const published = group.find((item) => item.status === STATUS.Published);
    const publishable = group
      .filter((item) => item.status === STATUS.Draft || item.status === STATUS.Unpublished)
      .sort((a, b) => b.id - a.id)[0];
    byTitle.set(key, published || publishable || group[0]);
  }
  return byTitle;
}

function validateBaseUrl(value) {
  let url;
  try {
    url = new URL(value);
  } catch {
    return 'GAMESPHERE_BASE_URL não é um URL válido.';
  }
  if (url.protocol !== 'http:' && url.protocol !== 'https:') {
    return 'GAMESPHERE_BASE_URL tem de usar http ou https.';
  }
  if (url.username || url.password) {
    return 'GAMESPHERE_BASE_URL não pode conter credenciais (utilizador ou password).';
  }
  const host = url.hostname;
  const loopback = host === 'localhost' || host === '[::1]' || /^127(\.\d{1,3}){3}$/.test(host);
  if (url.protocol === 'http:' && !loopback) {
    return 'GAMESPHERE_BASE_URL tem de usar https para hosts que não sejam localhost.';
  }
  return null;
}

function loadContent(contentDir) {
  const files = fs.readdirSync(contentDir).filter((name) => name.endsWith('.json')).sort();
  return files.map((fileName) => {
    try {
      const quiz = JSON.parse(fs.readFileSync(path.join(contentDir, fileName), 'utf8'));
      return { fileName, quiz };
    } catch (error) {
      return { fileName, parseError: error.message };
    }
  });
}

function parseArgs(args) {
  const options = { dryRun: false, publish: false, contentDir: null, unknown: [] };
  for (let i = 0; i < args.length; i += 1) {
    const arg = args[i];
    if (arg === '--dry-run') options.dryRun = true;
    else if (arg === '--publish') options.publish = true;
    else if (arg === '--content-dir') {
      options.contentDir = args[i + 1] || null;
      i += 1;
      if (!options.contentDir) options.unknown.push('--content-dir (sem valor)');
    } else options.unknown.push(arg);
  }
  return options;
}

async function run({ args = [], env = {}, contentDir, client, log = console.log } = {}) {
  const options = parseArgs(args);
  if (options.unknown.length > 0) {
    log(`Argumentos desconhecidos: ${options.unknown.join(', ')}`);
    log('Uso: load-curated-quizzes.js [--dry-run] [--publish] [--content-dir <pasta>]');
    return 1;
  }
  if (options.dryRun && options.publish) {
    log('--dry-run e --publish não podem ser usados em conjunto.');
    return 1;
  }

  const dir = options.contentDir || contentDir || DEFAULT_CONTENT_DIR;
  let entries;
  try {
    entries = loadContent(dir);
  } catch (error) {
    log(`Não foi possível ler ${dir}: ${error.message}`);
    return 1;
  }
  if (entries.length === 0) {
    log(`Nenhum ficheiro .json em ${dir}.`);
    return 1;
  }

  let invalid = 0;
  for (const entry of entries) {
    const errors = entry.parseError
      ? [`${entry.fileName}: JSON inválido (${entry.parseError})`]
      : validateQuiz(entry.quiz, entry.fileName);
    if (errors.length > 0) {
      invalid += 1;
      errors.forEach((error) => log(`ERRO ${error}`));
    } else {
      entry.valid = true;
      log(`OK ${entry.fileName}: ${entry.quiz.title} (${entry.quiz.questions.length} perguntas)`);
    }
  }
  const filesByTitle = new Map();
  for (const entry of entries) {
    if (!entry.valid) continue;
    const key = dotnetTrim(entry.quiz.title);
    filesByTitle.set(key, [...(filesByTitle.get(key) || []), entry.fileName]);
  }
  for (const [title, files] of filesByTitle) {
    if (files.length > 1) {
      invalid += 1;
      log(`ERRO título duplicado "${title}" nos ficheiros: ${files.join(', ')}`);
    }
  }
  if (invalid > 0) {
    log(`${invalid} ficheiro(s) inválido(s).`);
    return 1;
  }
  if (options.dryRun) {
    log(`${entries.length} ficheiro(s) válido(s).`);
    return 0;
  }

  const email = env.GAMESPHERE_ADMIN_EMAIL;
  const password = env.GAMESPHERE_ADMIN_PASSWORD;
  if (!email || !password) {
    log('Define GAMESPHERE_ADMIN_EMAIL e GAMESPHERE_ADMIN_PASSWORD.');
    return 1;
  }

  const baseUrl = env.GAMESPHERE_BASE_URL || DEFAULT_BASE_URL;
  const baseUrlError = validateBaseUrl(baseUrl);
  if (baseUrlError) {
    log(`ERRO ${baseUrlError}`);
    return 1;
  }

  const api = client || createApiClient({ baseUrl });
  let failures = 0;
  try {
    await api.login(email, password);
    const existing = await api.listQuizzes();
    const byTitle = pickByTitle(existing, options.publish);

    const counts = { created: 0, skipped: 0, published: 0, refused: 0, failed: 0 };
    let difference;
    for (const { quiz } of entries) {
      const title = dotnetTrim(quiz.title);
      const found = byTitle.get(title);

      if (!options.publish) {
        if (found) {
          counts.skipped += 1;
          log(`SALTADO "${title}": já existe (id ${found.id}, estado ${statusName(found.status)})`);
          continue;
        }
        try {
          const created = await api.createQuiz(toUpsertPayload(quiz));
          byTitle.set(title, created);
          counts.created += 1;
          log(`CRIADO "${title}": rascunho com id ${created.id}`);
        } catch (error) {
          counts.failed += 1;
          log(`FALHOU "${title}": ${error.message}`);
        }
        continue;
      }

      if (!found) {
        counts.refused += 1;
        log(`RECUSADO "${title}": não existe na API; carrega primeiro sem --publish`);
      } else if (found.status === STATUS.Published) {
        counts.skipped += 1;
        log(`SALTADO "${title}": já está publicado (id ${found.id})`);
      } else if (found.status !== STATUS.Draft && found.status !== STATUS.Unpublished) {
        counts.refused += 1;
        log(`RECUSADO "${title}": estado ${statusName(found.status)} não é publicável (id ${found.id})`);
      } else if (!isNonEmptyString(quiz.provenance.reviewedBy) || !isRealDate(quiz.provenance.reviewedAt)) {
        counts.refused += 1;
        log(`RECUSADO "${title}": sem revisão humana (provenance.reviewedBy e provenance.reviewedAt têm de estar preenchidos)`);
      } else if ((difference = findContentDifference(quiz, found)) !== null) {
        counts.refused += 1;
        log(`RECUSADO "${title}": o conteúdo no servidor (id ${found.id}) difere do ficheiro revisto em ${difference}; atualiza o rascunho no admin ou apaga-o e volta a carregar sem --publish`);
      } else {
        try {
          await api.changeStatus(found.id, STATUS.Published);
          counts.published += 1;
          log(`PUBLICADO "${title}" (id ${found.id})`);
        } catch (error) {
          counts.failed += 1;
          log(`FALHOU "${title}": ${error.message}`);
        }
      }
    }

    failures = counts.refused + counts.failed;
    log(options.publish
      ? `Publicados: ${counts.published}, recusados: ${counts.refused}, saltados: ${counts.skipped}, falhados: ${counts.failed}.`
      : `Criados: ${counts.created}, saltados: ${counts.skipped}, falhados: ${counts.failed}.`);
  } catch (error) {
    log(`ERRO ${error.message}`);
    return 1;
  }
  return failures > 0 ? 1 : 0;
}

module.exports = { validateQuiz, toUpsertPayload, findContentDifference, createApiClient, loadContent, run };

if (require.main === module) {
  run({ args: process.argv.slice(2), env: process.env })
    .then((code) => { process.exitCode = code; })
    .catch((error) => {
      console.error(`ERRO ${error.message}`);
      process.exitCode = 1;
    });
}
