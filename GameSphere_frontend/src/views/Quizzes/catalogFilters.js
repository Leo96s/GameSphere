import { isDifficulty, isQuizCategory } from '@/enums/quiz';

const firstValue = (value) => (Array.isArray(value) ? value[0] : value);

const parseEnumValue = (raw, isValid) => {
  const value = firstValue(raw);
  if (typeof value !== 'string' || !/^\d+$/.test(value)) return null;
  const parsed = Number(value);
  return isValid(parsed) ? parsed : null;
};

export const filtersFromQuery = (query) => ({
  category: parseEnumValue(query?.category, isQuizCategory),
  difficulty: parseEnumValue(query?.difficulty, isDifficulty),
});

export const queryFromFilters = (filters) => {
  const query = {};
  if (isQuizCategory(filters?.category)) query.category = String(filters.category);
  if (isDifficulty(filters?.difficulty)) query.difficulty = String(filters.difficulty);
  return query;
};

export const hasActiveFilters = (filters) =>
  isQuizCategory(filters?.category) || isDifficulty(filters?.difficulty);

export const sameFilters = (a, b) => a?.category === b?.category && a?.difficulty === b?.difficulty;
