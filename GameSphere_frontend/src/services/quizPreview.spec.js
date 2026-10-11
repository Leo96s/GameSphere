import { describe, expect, it } from 'vitest';
import { gradeQuizPreview } from './quizPreview';

const questions = [
  { id: 1, correctAnswer: 'Lisboa' },
  { id: 2, correctAnswer: 'Paris' },
];

describe('gradeQuizPreview', () => {
  it('conta todas as respostas certas', () => {
    const result = gradeQuizPreview(questions, { 1: 'Lisboa', 2: 'Paris' });

    expect(result.correctCount).toBe(2);
    expect(result.total).toBe(2);
    expect(result.feedback[1]).toEqual({ isCorrect: true, correctAnswer: 'Lisboa' });
    expect(result.feedback[2]).toEqual({ isCorrect: true, correctAnswer: 'Paris' });
  });

  it('conta zero quando tudo está errado e devolve a resposta correta', () => {
    const result = gradeQuizPreview(questions, { 1: 'Porto', 2: 'Lyon' });

    expect(result.correctCount).toBe(0);
    expect(result.feedback[1]).toEqual({ isCorrect: false, correctAnswer: 'Lisboa' });
    expect(result.feedback[2]).toEqual({ isCorrect: false, correctAnswer: 'Paris' });
  });

  it('mistura certas e erradas', () => {
    const result = gradeQuizPreview(questions, { 1: 'Lisboa', 2: 'Lyon' });

    expect(result.correctCount).toBe(1);
    expect(result.total).toBe(2);
    expect(result.feedback[1].isCorrect).toBe(true);
    expect(result.feedback[2].isCorrect).toBe(false);
  });

  it('ignora perguntas sem seleção, mas conta-as no total', () => {
    const result = gradeQuizPreview(questions, { 1: 'Lisboa' });

    expect(result.correctCount).toBe(1);
    expect(result.total).toBe(2);
    expect(result.feedback).not.toHaveProperty('2');
  });

  it('compara de forma exata, sem ignorar maiúsculas nem espaços', () => {
    const result = gradeQuizPreview(questions, { 1: 'lisboa', 2: 'Paris ' });

    expect(result.correctCount).toBe(0);
  });

  it('aceita um quiz sem perguntas', () => {
    expect(gradeQuizPreview([], {})).toEqual({ feedback: {}, correctCount: 0, total: 0 });
  });

  it('tolera entradas nulas e seleção vazia', () => {
    expect(gradeQuizPreview(undefined, undefined)).toEqual({ feedback: {}, correctCount: 0, total: 0 });
    expect(gradeQuizPreview(questions, undefined)).toEqual({ feedback: {}, correctCount: 0, total: 2 });
  });

  it('não trata uma resposta vazia como ausência de seleção', () => {
    const result = gradeQuizPreview(questions, { 1: '' });

    expect(result.feedback[1]).toEqual({ isCorrect: false, correctAnswer: 'Lisboa' });
  });

  it('ignora seleções de perguntas que não existem', () => {
    const result = gradeQuizPreview(questions, { 1: 'Lisboa', 99: 'x' });

    expect(result.correctCount).toBe(1);
    expect(result.feedback).not.toHaveProperty('99');
  });
});
