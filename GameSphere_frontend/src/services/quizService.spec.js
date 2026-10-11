import { beforeEach, describe, expect, it, vi } from 'vitest';

const apiMocks = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
}));

vi.mock('./api', () => ({
  default: apiMocks,
}));

import { getQuiz, getQuizPlayErrorMessage, isQuizUnavailableError, listQuizzes, submitAttempt } from './quizService';

describe('quizService', () => {
  beforeEach(() => vi.clearAllMocks());

  it('uses the protected quiz endpoints and returns their payloads', async () => {
    apiMocks.get
      .mockResolvedValueOnce({ data: [{ id: 1, title: 'Quiz' }] })
      .mockResolvedValueOnce({ data: { id: 1, questions: [] } });
    apiMocks.post.mockResolvedValueOnce({ data: { correctAnswers: 1 } });

    await expect(listQuizzes()).resolves.toEqual([{ id: 1, title: 'Quiz' }]);
    await expect(getQuiz(1)).resolves.toEqual({ id: 1, questions: [] });
    await expect(submitAttempt(1, { answers: [] })).resolves.toEqual({ correctAnswers: 1 });

    expect(apiMocks.get).toHaveBeenNthCalledWith(1, '/quizzes');
    expect(apiMocks.get).toHaveBeenNthCalledWith(2, '/quizzes/1');
    expect(apiMocks.post).toHaveBeenCalledWith('/quizzes/1/attempts', { answers: [] });
  });

  describe('getQuizPlayErrorMessage', () => {
    const fallback = 'Tenta novamente.';

    it('mostra que o quiz já não está disponível quando o pedido dá 404', () => {
      expect(getQuizPlayErrorMessage({ response: { status: 404, data: { message: 'Quiz not found.' } } }, fallback))
        .toBe('Este quiz já não está disponível.');
      expect(getQuizPlayErrorMessage({ response: { status: 404 } }, fallback))
        .toBe('Este quiz já não está disponível.');
    });

    it('mantém a mensagem do backend para os outros erros', () => {
      expect(getQuizPlayErrorMessage({ response: { status: 409, data: { message: 'Já submetido.' } } }, fallback))
        .toBe('Já submetido.');
    });

    it('usa o fallback quando não há resposta', () => {
      expect(getQuizPlayErrorMessage(new Error('rede'), fallback)).toBe(fallback);
      expect(getQuizPlayErrorMessage(undefined, fallback)).toBe(fallback);
    });
  });

  describe('isQuizUnavailableError', () => {
    it('identifica apenas respostas 404', () => {
      expect(isQuizUnavailableError({ response: { status: 404 } })).toBe(true);
      expect(isQuizUnavailableError({ response: { status: 409 } })).toBe(false);
      expect(isQuizUnavailableError(new Error('rede'))).toBe(false);
      expect(isQuizUnavailableError(undefined)).toBe(false);
    });
  });
});
