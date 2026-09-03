import { beforeEach, describe, expect, it, vi } from 'vitest';

const apiMocks = vi.hoisted(() => ({
  post: vi.fn(),
}));

vi.mock('./api', () => ({
  default: apiMocks,
}));

import {
  getCurrentRole,
  getCurrentUser,
  isAdmin,
  login,
  logout,
  recoverAccount,
  requestAccountRecovery,
  resetPassword,
  sentResetCode,
  social_login,
  validateResetCodeRequest,
} from './authService';

const storage = new Map();

Object.defineProperty(globalThis, 'localStorage', {
  configurable: true,
  value: {
    getItem: (key) => storage.get(key) ?? null,
    removeItem: (key) => storage.delete(key),
    setItem: (key, value) => storage.set(key, value),
  },
});

Object.defineProperty(globalThis, 'window', {
  configurable: true,
  value: { dispatchEvent: vi.fn() },
});

describe('authService', () => {
  beforeEach(() => {
    storage.clear();
    apiMocks.post.mockReset();
  });

  it('persists only the user returned by a password login', async () => {
    const user = { id: 7, email: 'player@example.test', role: 0 };
    apiMocks.post.mockResolvedValueOnce({ data: { user } });

    await expect(login(user.email, 'Test-password-123')).resolves.toEqual({ user });

    expect(apiMocks.post).toHaveBeenCalledWith('/User/login', {
      email: user.email,
      password: 'Test-password-123',
    });
    expect(JSON.parse(localStorage.getItem('user'))).toEqual(user);
    expect(localStorage.getItem('token')).toBeNull();
  });

  it('sends the Firebase ID token to the backend social-login endpoint', async () => {
    const user = { id: 8, email: 'social@example.test', role: 0 };
    apiMocks.post.mockResolvedValueOnce({ data: { user } });

    await social_login('firebase-id-token');

    expect(apiMocks.post).toHaveBeenCalledWith('/User/social-login', {
      idToken: 'firebase-id-token',
    });
    expect(JSON.parse(localStorage.getItem('user'))).toEqual(user);
  });

  it('rejects authentication responses without a user profile', async () => {
    apiMocks.post.mockResolvedValueOnce({ data: {} });

    await expect(login('player@example.test', 'Test-password-123'))
      .rejects.toThrow('sessão inválida');
    expect(localStorage.getItem('user')).toBeNull();
  });

  it('rejects social authentication responses without a user profile', async () => {
    apiMocks.post.mockResolvedValueOnce({ data: null });

    await expect(social_login('invalid-firebase-token'))
      .rejects.toThrow('sessão inválida');
  });

  it('clears the local profile and requests server-side logout', () => {
    storage.set('user', JSON.stringify({ id: 7, role: 0 }));
    apiMocks.post.mockResolvedValueOnce({ data: null });

    logout();

    expect(apiMocks.post).toHaveBeenCalledWith('/User/logout');
    expect(localStorage.getItem('user')).toBeNull();
  });

  it('does not surface a failed logout request to the caller', async () => {
    apiMocks.post.mockRejectedValueOnce(new Error('network failure'));

    expect(() => logout()).not.toThrow();
    await Promise.resolve();
    expect(localStorage.getItem('user')).toBeNull();
  });

  it('maps persisted roles and ignores malformed local profiles', () => {
    storage.set('user', JSON.stringify({ role: 1 }));
    expect(getCurrentUser()).toEqual({ role: 1 });
    expect(getCurrentRole()).toBe('Admin');
    expect(isAdmin()).toBe(true);

    storage.set('user', JSON.stringify({ role: 0 }));
    expect(getCurrentRole()).toBe('User');
    expect(isAdmin()).toBe(false);

    storage.set('user', JSON.stringify({ role: 'Guest' }));
    expect(getCurrentRole()).toBe('Guest');

    storage.set('user', 'undefined');
    expect(getCurrentUser()).toBeNull();
    storage.set('user', 'null');
    expect(getCurrentUser()).toBeNull();

    storage.set('user', '{invalid-json');
    expect(getCurrentUser()).toBeNull();
    expect(getCurrentRole()).toBeNull();
  });

  it('keeps password recovery requests on the API boundary', async () => {
    apiMocks.post
      .mockResolvedValueOnce({ data: { success: true } })
      .mockResolvedValueOnce({ data: { success: true } })
      .mockResolvedValueOnce({ data: { success: true } });

    await sentResetCode('player@example.test');
    await validateResetCodeRequest('player@example.test', '123456');
    await resetPassword('player@example.test', '123456', 'New-password-123');

    expect(apiMocks.post).toHaveBeenNthCalledWith(
      1,
      '/User/send-reset-code',
      'player@example.test',
      expect.objectContaining({ headers: { 'Content-Type': 'application/json' } }),
    );
    expect(apiMocks.post).toHaveBeenNthCalledWith(
      2,
      '/User/validate-reset-code',
      { email: 'player@example.test', resetCode: '123456' },
      expect.objectContaining({ headers: { 'Content-Type': 'application/json' } }),
    );
    expect(apiMocks.post).toHaveBeenNthCalledWith(
      3,
      '/User/reset-password',
      { email: 'player@example.test', resetCode: '123456', newPassword: 'New-password-123' },
      expect.objectContaining({ headers: { 'Content-Type': 'application/json' } }),
    );
  });

  it('exposes recovery errors without accepting empty responses', async () => {
    apiMocks.post
      .mockResolvedValueOnce({ data: { success: false, message: 'Invalid email' } })
      .mockResolvedValueOnce({ data: null })
      .mockResolvedValueOnce({ data: null });

    await expect(sentResetCode('missing@example.test')).rejects.toThrow('Invalid email');
    await expect(validateResetCodeRequest('player@example.test', '123456'))
      .rejects.toThrow('Erro ao validar código de recuperação');
    await expect(resetPassword('player@example.test', '123456', 'New-password-123'))
      .rejects.toThrow('Erro ao enviar código de recuperação');
  });

  it('keeps account recovery requests on the API boundary', async () => {
    apiMocks.post
      .mockResolvedValueOnce({ data: true })
      .mockResolvedValueOnce({ data: true });

    await expect(requestAccountRecovery('player@example.test')).resolves.toEqual(true);
    await expect(recoverAccount('player@example.test', '123456')).resolves.toEqual(true);

    expect(apiMocks.post).toHaveBeenNthCalledWith(
      1,
      '/User/request-account-recovery',
      'player@example.test',
      expect.objectContaining({ headers: { 'Content-Type': 'application/json' } }),
    );
    expect(apiMocks.post).toHaveBeenNthCalledWith(
      2,
      '/User/recover-account',
      { email: 'player@example.test', code: '123456' },
    );
  });
});
