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

describe('API request interceptor', () => {
  beforeEach(() => storage.clear());
  afterEach(() => storage.clear());

  it('adiciona o cabe\u00e7alho Bearer quando existe um token local', async () => {
    storage.set('token', 'test-token');
    const interceptor = api.interceptors.request.handlers.at(-1);

    expect(interceptor).toBeDefined();

    const request = await interceptor.fulfilled({ headers: {} });

    expect(request.headers.Authorization).toBe('Bearer test-token');
  });
});
