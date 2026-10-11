import api from './api';
import { translateAdminMessage } from './adminErrorMessages';

const readData = (request) => request.then((response) => response.data);

export const QUIZ_STATUS = Object.freeze({
  DRAFT: 0,
  PUBLISHED: 1,
  UNPUBLISHED: 2,
  ARCHIVED: 3,
});

export const quizStatusLabel = (status) => ({
  [QUIZ_STATUS.DRAFT]: 'Rascunho',
  [QUIZ_STATUS.PUBLISHED]: 'Publicado',
  [QUIZ_STATUS.UNPUBLISHED]: 'Retirado',
  [QUIZ_STATUS.ARCHIVED]: 'Arquivado',
}[status] ?? 'Desconhecido');

// Espelha as transições permitidas pelo backend (QuizStatusTransitions).
const QUIZ_STATUS_ACTIONS = Object.freeze({
  [QUIZ_STATUS.DRAFT]: [
    { status: QUIZ_STATUS.PUBLISHED, label: 'Publicar' },
    { status: QUIZ_STATUS.ARCHIVED, label: 'Arquivar' },
  ],
  [QUIZ_STATUS.PUBLISHED]: [
    { status: QUIZ_STATUS.UNPUBLISHED, label: 'Retirar' },
  ],
  [QUIZ_STATUS.UNPUBLISHED]: [
    { status: QUIZ_STATUS.PUBLISHED, label: 'Publicar' },
    { status: QUIZ_STATUS.ARCHIVED, label: 'Arquivar' },
  ],
  [QUIZ_STATUS.ARCHIVED]: [
    { status: QUIZ_STATUS.UNPUBLISHED, label: 'Restaurar' },
  ],
});

export const quizStatusActions = (status) => QUIZ_STATUS_ACTIONS[status] ?? [];

export const isQuizContentEditable = (status, hasAttempts = false) =>
  (status === QUIZ_STATUS.DRAFT || status === QUIZ_STATUS.UNPUBLISHED) && !hasAttempts;

export const listAdminQuizzes = ({ includeArchived = false } = {}) => readData(includeArchived
  ? api.get('/admin/quizzes', { params: { includeArchived: true } })
  : api.get('/admin/quizzes'));

export const getAdminQuiz = (id) => readData(api.get(`/admin/quizzes/${id}`));

export const createQuiz = (quiz) => readData(api.post('/admin/quizzes', quiz));

export const updateQuiz = (id, quiz) => readData(api.put(`/admin/quizzes/${id}`, quiz));

export const deleteQuiz = (id) => readData(api.delete(`/admin/quizzes/${id}`));

export const changeQuizStatus = (id, status) =>
  readData(api.post(`/admin/quizzes/${id}/status`, { status }));

export const createQuestion = (quizId, question) =>
  readData(api.post(`/admin/quizzes/${quizId}/questions`, question));

export const updateQuestion = (id, question) =>
  readData(api.put(`/admin/questions/${id}`, question));

export const deleteQuestion = (id) => readData(api.delete(`/admin/questions/${id}`));

export const getAdminErrorMessage = (error) => {
  if (error?.response?.status === 403) {
    return 'Não tens permissão para gerir quizzes.';
  }

  if (error?.response?.status === 401) {
    return 'A tua sessão terminou. Inicia sessão novamente.';
  }

  const backendMessage = error?.response?.data?.message ?? error?.response?.data?.detail;

  return translateAdminMessage(
    backendMessage,
    (name) => quizStatusLabel(QUIZ_STATUS[name.toUpperCase()]),
  ) ?? 'Não foi possível concluir a alteração. Tenta novamente.';
};

export const validateQuizDetails = ({ title, difficulty } = {}) => {
  const trimmedTitle = typeof title === 'string' ? title.trim() : '';

  if (!trimmedTitle) {
    return 'Indica o título do quiz.';
  }

  if (trimmedTitle.length > 100) {
    return 'O título do quiz não pode ter mais de 100 caracteres.';
  }

  if (![0, 1, 2].includes(Number(difficulty))) {
    return 'Escolhe uma dificuldade válida.';
  }

  return '';
};
