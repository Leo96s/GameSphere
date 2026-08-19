import { describe, expect, it, vi } from 'vitest';

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
  createQuestion,
  createQuiz,
  deleteQuestion,
  deleteQuiz,
  getAdminErrorMessage,
  listAdminQuizzes,
  updateQuestion,
  updateQuiz,
} from './adminQuizService';
import { validateQuestion } from '@/views/Admin/AdminQuestionEditor.vue';

describe('adminQuizService', () => {
  it('usa os endpoints administrativos para gerir quizzes e perguntas', async () => {
    apiMocks.get.mockResolvedValueOnce({ data: [{ id: 1, title: 'História' }] });
    apiMocks.post.mockResolvedValue({ data: { id: 1 } });
    apiMocks.put.mockResolvedValue({ data: { id: 1 } });
    apiMocks.remove.mockResolvedValue({ data: undefined });

    await expect(listAdminQuizzes()).resolves.toEqual([{ id: 1, title: 'História' }]);
    await createQuiz({ title: 'História', difficulty: 0, isPublished: false });
    await updateQuiz(1, { title: 'História', difficulty: 0, isPublished: true });
    await deleteQuiz(1);
    await createQuestion(1, { description: 'Quando?', answers: ['A', 'B'], correctAnswer: 'A', typeOfAnswer: 0 });
    await updateQuestion(2, { description: 'Quando?', answers: ['A', 'B'], correctAnswer: 'A', typeOfAnswer: 0 });
    await deleteQuestion(2);

    expect(apiMocks.get).toHaveBeenCalledWith('/admin/quizzes');
    expect(apiMocks.post).toHaveBeenNthCalledWith(1, '/admin/quizzes', expect.any(Object));
    expect(apiMocks.put).toHaveBeenNthCalledWith(1, '/admin/quizzes/1', expect.objectContaining({ isPublished: true }));
    expect(apiMocks.remove).toHaveBeenNthCalledWith(1, '/admin/quizzes/1');
    expect(apiMocks.post).toHaveBeenNthCalledWith(2, '/admin/quizzes/1/questions', expect.any(Object));
    expect(apiMocks.put).toHaveBeenNthCalledWith(2, '/admin/questions/2', expect.any(Object));
    expect(apiMocks.remove).toHaveBeenNthCalledWith(2, '/admin/questions/2');
  });

  it('explica quando uma resposta correta não está nas opções', () => {
    expect(validateQuestion({ answers: ['A', 'B'], correctAnswer: 'C' }))
      .toBe('A resposta correta tem de constar nas opções.');
  });

  it('mantém o erro 403 visível para a interface', () => {
    expect(getAdminErrorMessage({ response: { status: 403 } }))
      .toBe('Não tens permissão para gerir quizzes.');
  });
});
