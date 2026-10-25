import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import {
  DIFFICULTIES,
  QUIZ_CATEGORIES,
  difficultyLabel,
  isDifficulty,
  isQuizCategory,
  quizCategoryLabel,
} from './quiz';

describe('quiz enums', () => {
  it('lists the eight categories with the backend values and PT-PT labels', () => {
    expect(QUIZ_CATEGORIES).toEqual([
      { value: 0, label: 'Cultura geral' },
      { value: 1, label: 'Geografia' },
      { value: 2, label: 'História' },
      { value: 3, label: 'Ciência' },
      { value: 4, label: 'Desporto' },
      { value: 5, label: 'Entretenimento' },
      { value: 6, label: 'Tecnologia' },
      { value: 7, label: 'Videojogos' },
    ]);
  });

  it('lists the three difficulties', () => {
    expect(DIFFICULTIES).toEqual([
      { value: 0, label: 'Fácil' },
      { value: 1, label: 'Médio' },
      { value: 2, label: 'Difícil' },
    ]);
  });

  it('resolves labels and falls back for unknown values', () => {
    expect(quizCategoryLabel(1)).toBe('Geografia');
    expect(quizCategoryLabel(0)).toBe('Cultura geral');
    expect(quizCategoryLabel(99)).toBe('Sem categoria definida');
    expect(quizCategoryLabel(undefined)).toBe('Sem categoria definida');
    expect(difficultyLabel(2)).toBe('Difícil');
    expect(difficultyLabel(9)).toBe('Sem dificuldade definida');
  });

  it('validates only integer values that belong to each enum', () => {
    expect(isQuizCategory(0)).toBe(true);
    expect(isQuizCategory(7)).toBe(true);
    expect(isQuizCategory(8)).toBe(false);
    expect(isQuizCategory(-1)).toBe(false);
    expect(isQuizCategory(1.5)).toBe(false);
    expect(isQuizCategory('1')).toBe(false);
    expect(isQuizCategory(null)).toBe(false);
    expect(isDifficulty(2)).toBe(true);
    expect(isDifficulty(3)).toBe(false);
    expect(isDifficulty(undefined)).toBe(false);
  });

  const backendEnum = (file) => {
    const text = readFileSync(resolve(__dirname, '../../../GameSphere_backend/Enums', file), 'utf8');
    const body = text.match(/enum\s+\w+\s*\{([^}]*)\}/)[1];
    return body.split(',').map((name) => name.trim()).filter(Boolean);
  };

  it('keeps values in the same order as the backend enums', () => {
    expect(backendEnum('QuizCategory.cs')).toHaveLength(QUIZ_CATEGORIES.length);
    expect(backendEnum('Difficulty.cs')).toHaveLength(DIFFICULTIES.length);
    expect(QUIZ_CATEGORIES.map((c) => c.value)).toEqual(QUIZ_CATEGORIES.map((_, i) => i));
    expect(backendEnum('QuizCategory.cs')[1]).toBe('GEOGRAPHY');
    expect(backendEnum('QuizCategory.cs')[7]).toBe('VIDEO_GAMES');
  });

  it('exposes immutable option lists', () => {
    expect(Object.isFrozen(QUIZ_CATEGORIES)).toBe(true);
    expect(Object.isFrozen(DIFFICULTIES)).toBe(true);
  });

  it('leaves no local difficultyLabel copies in the views', () => {
    const walk = (dir) => readdirSync(dir).flatMap((entry) => {
      const full = join(dir, entry);
      return statSync(full).isDirectory() ? walk(full) : [full];
    });
    const offenders = walk(resolve(__dirname, '..'))
      .filter((file) => /\.(vue|js)$/.test(file) && !/\.spec\.js$/.test(file) && !file.endsWith(join('enums', 'quiz.js')))
      .filter((file) => /(const|function)\s+difficultyLabel\b/.test(readFileSync(file, 'utf8')));
    expect(offenders).toEqual([]);
  });
});
