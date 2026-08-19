import { describe, expect, it } from 'vitest';
import { useQuizAttempt } from './useQuizAttempt';

const quiz = {
  id: 12,
  questions: [
    { id: 4, description: 'Capital de Portugal?', answers: ['Lisboa', 'Porto'] },
    { id: 9, description: 'Capital de Fran\u00e7a?', answers: ['Paris', 'Lyon'] },
  ],
};

describe('useQuizAttempt', () => {
  it('substitui a resposta de uma pergunta sem criar uma resposta adicional', () => {
    const attempt = useQuizAttempt(quiz);

    attempt.selectAnswer(4, 'Porto');
    attempt.selectAnswer(4, 'Lisboa');

    expect(attempt.toRequest()).toEqual({
      answers: [{ questionId: 4, selectedAnswer: 'Lisboa' }],
    });
  });

  it('s\u00f3 fica completo quando todas as perguntas t\u00eam uma resposta', () => {
    const attempt = useQuizAttempt(quiz);

    attempt.selectAnswer(4, 'Lisboa');
    expect(attempt.isComplete.value).toBe(false);

    attempt.selectAnswer(9, 'Paris');
    expect(attempt.isComplete.value).toBe(true);
  });

  it('n\u00e3o fica completo quando o quiz n\u00e3o tem perguntas', () => {
    const attempt = useQuizAttempt({ id: 13, questions: [] });

    expect(attempt.isComplete.value).toBe(false);
    expect(attempt.toRequest()).toEqual({ answers: [] });
  });

  it('cria o payload de submiss\u00e3o a partir das respostas selecionadas', () => {
    const attempt = useQuizAttempt(quiz);

    attempt.selectAnswer(4, 'Lisboa');
    attempt.selectAnswer(9, 'Paris');

    expect(attempt.toRequest()).toEqual({
      answers: [
        { questionId: 4, selectedAnswer: 'Lisboa' },
        { questionId: 9, selectedAnswer: 'Paris' },
      ],
    });
  });

  it('limpa as respostas depois de apresentar o resultado', () => {
    const attempt = useQuizAttempt(quiz);

    attempt.selectAnswer(4, 'Lisboa');
    attempt.clear();

    expect(attempt.toRequest()).toEqual({ answers: [] });
    expect(attempt.isComplete.value).toBe(false);
  });
});
