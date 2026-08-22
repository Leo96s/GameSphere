import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import api from './api';

const storage = new Map();

Object.defineProperty(globalThis, 'localStorage', {
  configurable: true,
  value: {
    getItem: (key) => storage.get(key) ?? null,
    removeItem: (key) => storage.delete(key),
    setItem: (key, value) => storage.set(key, value),
  },
});

describe('API client', () => {
  beforeEach(() => storage.clear());
  afterEach(() => storage.clear());

  it('sends credentials for the HttpOnly cookie session', () => {
    expect(api.defaults.withCredentials).toBe(true);
    expect(api.interceptors.request.handlers).toHaveLength(0);
  });
});
