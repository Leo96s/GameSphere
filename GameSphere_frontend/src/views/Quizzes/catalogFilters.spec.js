import { describe, expect, it } from 'vitest';
import { filtersFromQuery, hasActiveFilters, queryFromFilters, sameFilters } from './catalogFilters';

describe('catalogFilters', () => {
  it('compares filters by value', () => {
    expect(sameFilters({ category: 1, difficulty: null }, { category: 1, difficulty: null })).toBe(true);
    expect(sameFilters({ category: 1, difficulty: null }, { category: 1, difficulty: 0 })).toBe(false);
    expect(sameFilters({ category: null, difficulty: null }, { category: 0, difficulty: null })).toBe(false);
  });

  it('reads valid category and difficulty from the query', () => {
    expect(filtersFromQuery({ category: '1', difficulty: '0' })).toEqual({ category: 1, difficulty: 0 });
    expect(filtersFromQuery({ category: '0' })).toEqual({ category: 0, difficulty: null });
  });

  it('ignores missing, invalid and out-of-range values', () => {
    expect(filtersFromQuery({})).toEqual({ category: null, difficulty: null });
    expect(filtersFromQuery(undefined)).toEqual({ category: null, difficulty: null });
    expect(filtersFromQuery({ category: '99', difficulty: '3' })).toEqual({ category: null, difficulty: null });
    expect(filtersFromQuery({ category: 'abc', difficulty: '' })).toEqual({ category: null, difficulty: null });
    expect(filtersFromQuery({ category: '1.5', difficulty: '-1' })).toEqual({ category: null, difficulty: null });
    expect(filtersFromQuery({ category: '1e0', difficulty: ' 1' })).toEqual({ category: null, difficulty: null });
  });

  it('uses the first value when the query repeats a parameter', () => {
    expect(filtersFromQuery({ category: ['2', '3'] })).toEqual({ category: 2, difficulty: null });
  });

  it('builds a query with only the active filters', () => {
    expect(queryFromFilters({ category: 1, difficulty: 0 })).toEqual({ category: '1', difficulty: '0' });
    expect(queryFromFilters({ category: 0, difficulty: null })).toEqual({ category: '0' });
    expect(queryFromFilters({ category: null, difficulty: null })).toEqual({});
    expect(queryFromFilters({ category: 99, difficulty: 2 })).toEqual({ difficulty: '2' });
  });

  it('reports whether any filter is active', () => {
    expect(hasActiveFilters({ category: null, difficulty: null })).toBe(false);
    expect(hasActiveFilters({ category: 0, difficulty: null })).toBe(true);
    expect(hasActiveFilters({ category: null, difficulty: 2 })).toBe(true);
  });

  it('round-trips every valid combination through the URL query', () => {
    for (const category of [null, 0, 1, 7]) {
      for (const difficulty of [null, 0, 1, 2]) {
        const filters = { category, difficulty };
        expect(filtersFromQuery(queryFromFilters(filters))).toEqual(filters);
      }
    }
  });

  it('keeps the valid filter when the other one is invalid', () => {
    expect(filtersFromQuery({ category: '1', difficulty: '9' })).toEqual({ category: 1, difficulty: null });
    expect(filtersFromQuery({ category: 'x', difficulty: '2' })).toEqual({ category: null, difficulty: 2 });
  });

  it('tolerates null, empty and non-string query values', () => {
    expect(filtersFromQuery(null)).toEqual({ category: null, difficulty: null });
    expect(filtersFromQuery({ category: null, difficulty: [] })).toEqual({ category: null, difficulty: null });
    expect(filtersFromQuery({ category: 1, difficulty: 1 })).toEqual({ category: null, difficulty: null });
    expect(queryFromFilters(undefined)).toEqual({});
    expect(hasActiveFilters(undefined)).toBe(false);
    expect(hasActiveFilters({ category: 99, difficulty: 7 })).toBe(false);
  });
});
