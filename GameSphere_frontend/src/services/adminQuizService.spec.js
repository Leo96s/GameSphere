import { beforeEach, describe, expect, it, vi } from 'vitest';

const apiMocks = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  remove: vi.fn(),
}));

vi.mock('./api', () => ({
  default: {
    get: apiMocks.get,
    post: apiMocks.post,
    put: apiMocks.put,
    delete: apiMocks.remove,
  },
}));

import {
  QUIZ_STATUS,
  changeQuizStatus,
  createQuestion,
  createQuiz,
  deleteQuestion,
  deleteQuiz,
  getAdminErrorMessage,
  getAdminQuiz,
  isQuizContentEditable,
  listAdminQuizzes,
  quizStatusActions,
  quizStatusLabel,
  updateQuestion,
  updateQuiz,
  validateQuizDetails,
} from './adminQuizService';
import { validateQuestion } from '@/views/Admin/AdminQuestionEditor.vue';

describe('adminQuizService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('usa os endpoints administrativos para gerir quizzes e perguntas', async () => {
    apiMocks.get.mockResolvedValueOnce({ data: [{ id: 1, title: 'História' }] });
    apiMocks.post.mockResolvedValue({ data: { id: 1 } });
    apiMocks.put.mockResolvedValue({ data: { id: 1 } });
    apiMocks.remove.mockResolvedValue({ data: undefined });

    await expect(listAdminQuizzes()).resolves.toEqual([{ id: 1, title: 'História' }]);
    await createQuiz({ title: 'História', difficulty: 0 });
    await updateQuiz(1, { title: 'História', difficulty: 1 });
    await deleteQuiz(1);
    await createQuestion(1, { description: 'Quando?', answers: ['A', 'B'], correctAnswer: 'A', typeOfAnswer: 0 });
    await updateQuestion(2, { description: 'Quando?', answers: ['A', 'B'], correctAnswer: 'A', typeOfAnswer: 0 });
    await deleteQuestion(2);

    expect(apiMocks.get).toHaveBeenCalledWith('/admin/quizzes');
    expect(apiMocks.post).toHaveBeenNthCalledWith(1, '/admin/quizzes', expect.any(Object));
    expect(apiMocks.post).toHaveBeenNthCalledWith(1, '/admin/quizzes', { title: 'História', difficulty: 0 });
    expect(apiMocks.put).toHaveBeenNthCalledWith(1, '/admin/quizzes/1', { title: 'História', difficulty: 1 });
    expect(apiMocks.remove).toHaveBeenNthCalledWith(1, '/admin/quizzes/1');
    expect(apiMocks.post).toHaveBeenNthCalledWith(2, '/admin/quizzes/1/questions', expect.any(Object));
    expect(apiMocks.put).toHaveBeenNthCalledWith(2, '/admin/questions/2', expect.any(Object));
    expect(apiMocks.remove).toHaveBeenNthCalledWith(2, '/admin/questions/2');
  });

  it('explica quando uma resposta correta não está nas opções', () => {
    expect(validateQuestion({ description: 'Qual?', answers: ['A', 'B'], correctAnswer: 'C' }))
      .toBe('A resposta correta tem de constar nas opções.');
  });

  it('mantém o erro 403 visível para a interface', () => {
    expect(getAdminErrorMessage({ response: { status: 403 } }))
      .toBe('Não tens permissão para gerir quizzes.');
  });

  it('mapeia os restantes erros administrativos para mensagens acionáveis', () => {
    expect(getAdminErrorMessage({ response: { status: 401 } }))
      .toBe('A tua sessão terminou. Inicia sessão novamente.');
    expect(getAdminErrorMessage({ response: { data: { message: 'Validation failed' } } }))
      .toBe('Não foi possível concluir a alteração. Tenta novamente.');
    expect(getAdminErrorMessage({ response: { data: { detail: 'Details' } } }))
      .toBe('Não foi possível concluir a alteração. Tenta novamente.');
    expect(getAdminErrorMessage({})).toBe('Não foi possível concluir a alteração. Tenta novamente.');
  });

  it('lista quizzes sem arquivados por omissão e com arquivados quando pedido', async () => {
    apiMocks.get.mockResolvedValue({ data: [] });

    await listAdminQuizzes();
    await listAdminQuizzes({ includeArchived: false });
    await listAdminQuizzes({ includeArchived: true });

    expect(apiMocks.get).toHaveBeenNthCalledWith(1, '/admin/quizzes');
    expect(apiMocks.get).toHaveBeenNthCalledWith(2, '/admin/quizzes');
    expect(apiMocks.get).toHaveBeenNthCalledWith(3, '/admin/quizzes', { params: { includeArchived: true } });
  });

  it('carrega um quiz administrativo pelo id', async () => {
    apiMocks.get.mockResolvedValueOnce({ data: { id: 7, status: QUIZ_STATUS.DRAFT } });

    await expect(getAdminQuiz(7)).resolves.toEqual({ id: 7, status: QUIZ_STATUS.DRAFT });
    expect(apiMocks.get).toHaveBeenCalledWith('/admin/quizzes/7');
  });

  it('muda o estado pelo endpoint dedicado, só com o estado no corpo', async () => {
    apiMocks.post.mockResolvedValueOnce({ data: { id: 3, status: QUIZ_STATUS.PUBLISHED } });

    await expect(changeQuizStatus(3, QUIZ_STATUS.PUBLISHED)).resolves.toEqual({ id: 3, status: QUIZ_STATUS.PUBLISHED });
    expect(apiMocks.post).toHaveBeenCalledWith('/admin/quizzes/3/status', { status: 1 });
  });

  it('propaga o erro do endpoint de estado para a interface mostrar a mensagem do backend', async () => {
    const error = { response: { status: 409, data: { message: 'Cannot change quiz status from Published to Archived.' } } };
    apiMocks.post.mockRejectedValueOnce(error);

    await expect(changeQuizStatus(3, QUIZ_STATUS.ARCHIVED)).rejects.toBe(error);
    expect(getAdminErrorMessage(error)).toBe('Não é possível mudar o estado de «Publicado» para «Arquivado».');
    expect(getAdminErrorMessage({ response: { status: 400, data: { message: 'Question 2: The correct answer must be one of the question options.' } } }))
      .toBe('Pergunta 2: A resposta correta tem de constar nas opções.');
  });

  it('usa os mesmos valores numéricos de estado que o backend', () => {
    expect(QUIZ_STATUS).toEqual({ DRAFT: 0, PUBLISHED: 1, UNPUBLISHED: 2, ARCHIVED: 3 });
  });

  it('etiqueta os quatro estados em PT-PT', () => {
    expect(quizStatusLabel(QUIZ_STATUS.DRAFT)).toBe('Rascunho');
    expect(quizStatusLabel(QUIZ_STATUS.PUBLISHED)).toBe('Publicado');
    expect(quizStatusLabel(QUIZ_STATUS.UNPUBLISHED)).toBe('Retirado');
    expect(quizStatusLabel(QUIZ_STATUS.ARCHIVED)).toBe('Arquivado');
    expect(quizStatusLabel(99)).toBe('Desconhecido');
    expect(quizStatusLabel(undefined)).toBe('Desconhecido');
  });

  it('oferece só as transições permitidas pelo backend', () => {
    const targets = (status) => quizStatusActions(status).map((action) => [action.status, action.label]);

    expect(targets(QUIZ_STATUS.DRAFT)).toEqual([[QUIZ_STATUS.PUBLISHED, 'Publicar'], [QUIZ_STATUS.ARCHIVED, 'Arquivar']]);
    expect(targets(QUIZ_STATUS.PUBLISHED)).toEqual([[QUIZ_STATUS.UNPUBLISHED, 'Retirar']]);
    expect(targets(QUIZ_STATUS.UNPUBLISHED)).toEqual([[QUIZ_STATUS.PUBLISHED, 'Publicar'], [QUIZ_STATUS.ARCHIVED, 'Arquivar']]);
    expect(targets(QUIZ_STATUS.ARCHIVED)).toEqual([[QUIZ_STATUS.UNPUBLISHED, 'Restaurar']]);
    expect(quizStatusActions(99)).toEqual([]);
    expect(quizStatusActions(undefined)).toEqual([]);
  });

  it('só permite editar conteúdo em rascunho ou retirado', () => {
    expect(isQuizContentEditable(QUIZ_STATUS.DRAFT)).toBe(true);
    expect(isQuizContentEditable(QUIZ_STATUS.UNPUBLISHED)).toBe(true);
    expect(isQuizContentEditable(QUIZ_STATUS.PUBLISHED)).toBe(false);
    expect(isQuizContentEditable(QUIZ_STATUS.ARCHIVED)).toBe(false);
    expect(isQuizContentEditable(undefined)).toBe(false);
  });

  it('bloqueia a edição de conteúdo quando o quiz já tem tentativas', () => {
    expect(isQuizContentEditable(QUIZ_STATUS.DRAFT, true)).toBe(false);
    expect(isQuizContentEditable(QUIZ_STATUS.UNPUBLISHED, true)).toBe(false);
    expect(isQuizContentEditable(QUIZ_STATUS.PUBLISHED, true)).toBe(false);
    expect(isQuizContentEditable(QUIZ_STATUS.ARCHIVED, true)).toBe(false);
  });

  it('permite editar rascunho ou retirado quando não há tentativas', () => {
    expect(isQuizContentEditable(QUIZ_STATUS.DRAFT, false)).toBe(true);
    expect(isQuizContentEditable(QUIZ_STATUS.UNPUBLISHED, false)).toBe(true);
    expect(isQuizContentEditable(QUIZ_STATUS.PUBLISHED, false)).toBe(false);
    expect(isQuizContentEditable(QUIZ_STATUS.ARCHIVED, false)).toBe(false);
    expect(isQuizContentEditable(QUIZ_STATUS.DRAFT, undefined)).toBe(true);
  });

  it('mostra a mensagem do backend nos 400 uniformes { message }', () => {
    const error = { response: { status: 400, data: { message: 'The request body is invalid.' } } };
    expect(getAdminErrorMessage(error)).toBe('Os dados enviados não são válidos.');
  });

  it('traduz uma mensagem conhecida com prefixo de pergunta desconhecida para o fallback', () => {
    expect(getAdminErrorMessage({ response: { status: 400, data: { message: 'Question 3: Something else.' } } }))
      .toBe('Não foi possível concluir a alteração. Tenta novamente.');
  });

  it('valida os detalhes do quiz antes de gravar', () => {
    expect(validateQuizDetails({ title: '', difficulty: 0 })).toBe('Indica o título do quiz.');
    expect(validateQuizDetails({ title: '   ', difficulty: 0 })).toBe('Indica o título do quiz.');
    expect(validateQuizDetails({ difficulty: 0 })).toBe('Indica o título do quiz.');
    expect(validateQuizDetails()).toBe('Indica o título do quiz.');
    expect(validateQuizDetails({ title: 'A'.repeat(101), difficulty: 0 }))
      .toBe('O título do quiz não pode ter mais de 100 caracteres.');
    expect(validateQuizDetails({ title: 'Quiz', difficulty: 3 })).toBe('Escolhe uma dificuldade válida.');
    expect(validateQuizDetails({ title: 'Quiz', difficulty: undefined })).toBe('Escolhe uma dificuldade válida.');
    expect(validateQuizDetails({ title: 'A'.repeat(100), difficulty: 2 })).toBe('');
    expect(validateQuizDetails({ title: '  Quiz  ', difficulty: 1 })).toBe('');
  });

  it('valida a pergunta pela ordem enunciado, opções, duplicados e resposta correta', () => {
    const valid = { description: 'Capital?', answers: ['Lisboa', 'Porto'], correctAnswer: 'Lisboa' };

    expect(validateQuestion({ ...valid, description: '  ' })).toBe('Escreve a pergunta antes de a guardar.');
    expect(validateQuestion({ ...valid, description: undefined })).toBe('Escreve a pergunta antes de a guardar.');
    expect(validateQuestion({ ...valid, answers: ['Lisboa', ' '] })).toBe('Indica pelo menos duas opções.');
    expect(validateQuestion({ ...valid, answers: ['Paris', ' paris '], correctAnswer: 'Paris' }))
      .toBe('Cada opção tem de ser diferente.');
    expect(validateQuestion({ ...valid, correctAnswer: 'Faro' })).toBe('A resposta correta tem de constar nas opções.');
    expect(validateQuestion(valid)).toBe('');
  });
});
