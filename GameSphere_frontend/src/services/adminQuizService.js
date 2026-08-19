import api from './api';

const readData = (request) => request.then((response) => response.data);

export const listAdminQuizzes = () => readData(api.get('/admin/quizzes'));

export const createQuiz = (quiz) => readData(api.post('/admin/quizzes', quiz));

export const updateQuiz = (id, quiz) => readData(api.put(`/admin/quizzes/${id}`, quiz));

export const deleteQuiz = (id) => readData(api.delete(`/admin/quizzes/${id}`));

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

  return error?.response?.data?.message
    ?? error?.response?.data?.detail
    ?? 'Não foi possível concluir a alteração. Tenta novamente.';
};
