import { beforeEach, describe, expect, it, vi } from 'vitest';

const apiMocks = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
}));

vi.mock('./api', () => ({
  default: apiMocks,
}));

import { getQuiz, listQuizzes, submitAttempt } from './quizService';

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
});
