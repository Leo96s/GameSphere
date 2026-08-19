import api from './api';

export const listQuizzes = async () => {
  const response = await api.get('/quizzes');
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
