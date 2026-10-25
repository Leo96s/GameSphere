export const QUIZ_CATEGORIES = Object.freeze([
  { value: 0, label: 'Cultura geral' },
  { value: 1, label: 'Geografia' },
  { value: 2, label: 'História' },
  { value: 3, label: 'Ciência' },
  { value: 4, label: 'Desporto' },
  { value: 5, label: 'Entretenimento' },
  { value: 6, label: 'Tecnologia' },
  { value: 7, label: 'Videojogos' },
]);

export const DIFFICULTIES = Object.freeze([
  { value: 0, label: 'Fácil' },
  { value: 1, label: 'Médio' },
  { value: 2, label: 'Difícil' },
]);

const labelOf = (options, value, fallback) =>
  options.find((option) => option.value === value)?.label ?? fallback;

const isOneOf = (options, value) => Number.isInteger(value) && options.some((option) => option.value === value);

export const quizCategoryLabel = (category) => labelOf(QUIZ_CATEGORIES, category, 'Sem categoria definida');

export const difficultyLabel = (difficulty) => labelOf(DIFFICULTIES, difficulty, 'Sem dificuldade definida');

export const isQuizCategory = (value) => isOneOf(QUIZ_CATEGORIES, value);

export const isDifficulty = (value) => isOneOf(DIFFICULTIES, value);
