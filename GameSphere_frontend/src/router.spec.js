import { describe, expect, it, vi } from 'vitest';

vi.mock('vue-router', () => ({
  createRouter: () => ({ beforeEach: vi.fn() }),
  createWebHistory: vi.fn(),
}));

import { createNavigationGuard, routes } from './router';

describe('createNavigationGuard', () => {
  it('redireciona para a landing sem token numa rota autenticada', () => {
    const guard = createNavigationGuard({
      getToken: () => null,
      getCurrentRole: () => null,
    });

    expect(guard({ meta: { requiresAuth: true } })).toEqual({ name: 'landing' });
  });

  it('bloqueia um User numa rota de administra\u00e7\u00e3o', () => {
    const guard = createNavigationGuard({
      getToken: () => 'test-token',
      getCurrentRole: () => 'User',
    });

    expect(guard({ meta: { requiresAdmin: true } })).toEqual({ name: 'landing' });
  });

  it('permite um Admin numa rota de administra\u00e7\u00e3o', () => {
    const guard = createNavigationGuard({
      getToken: () => 'test-token',
      getCurrentRole: () => 'Admin',
    });

    expect(guard({ meta: { requiresAdmin: true } })).toBe(true);
  });
});

describe('quiz result route', () => {
  it('identifica o quiz no caminho do resultado', () => {
    const resultRoute = routes.find((route) => route.name === 'quiz-result');

    expect(resultRoute).toMatchObject({
      path: '/quizzes/:id/result',
      meta: { requiresAuth: true },
    });
  });
});
