import api from './api';

export const listQuizzes = async (filters = {}) => {
  const params = {};
  if (filters.category !== undefined && filters.category !== null) params.category = filters.category;
  if (filters.difficulty !== undefined && filters.difficulty !== null) params.difficulty = filters.difficulty;

  const response = Object.keys(params).length > 0
    ? await api.get('/quizzes', { params })
    : await api.get('/quizzes');
  return response.data;
};

export const getQuiz = async (id) => {
  const response = await api.get(`/quizzes/${id}`);
  return response.data;
};

export const submitAttempt = async (id, answers) => {
  const response = await api.post(`/quizzes/${id}/attempts`, answers);
  return response.data;
};

export const QUIZ_UNAVAILABLE_MESSAGE = 'Este quiz já não está disponível.';

export const isQuizUnavailableError = (error) => error?.response?.status === 404;

export const getQuizPlayErrorMessage = (error, fallback) =>
  isQuizUnavailableError(error)
    ? QUIZ_UNAVAILABLE_MESSAGE
    : (error?.response?.data?.message ?? fallback);
