import { beforeEach, describe, expect, it, vi } from 'vitest';

const apiMocks = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  delete: vi.fn(),
}));

vi.mock('./api', () => ({
  default: apiMocks,
}));

import {
  createUser,
  deleteUser,
  editUser,
  getUser,
  getUserByEmail,
  getUsers,
} from './userServices';

describe('userServices', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('uses cookie-backed API calls for account operations', async () => {
    apiMocks.get
      .mockResolvedValueOnce({ data: [{ id: 1 }] })
      .mockResolvedValueOnce({ data: { id: 1 } })
      .mockResolvedValueOnce({ data: { id: 1 } });
    apiMocks.post.mockResolvedValueOnce({ data: { id: 2 } });
    apiMocks.put.mockResolvedValueOnce({ data: { id: 1 } });
    apiMocks.delete.mockResolvedValueOnce({ data: undefined });

    await expect(getUsers()).resolves.toEqual([{ id: 1 }]);
    await expect(getUser(1)).resolves.toEqual({ id: 1 });
    await expect(createUser({ email: 'player@example.test' })).resolves.toEqual({ id: 2 });
    await expect(editUser(1, { firstName: 'Updated' })).resolves.toEqual({ id: 1 });
    await expect(deleteUser(1)).resolves.toBeUndefined();
    await expect(getUserByEmail('Player+test@example.test')).resolves.toEqual({ id: 1 });

    expect(apiMocks.get).toHaveBeenNthCalledWith(1, '/User');
    expect(apiMocks.get).toHaveBeenNthCalledWith(2, '/User/by-id/1');
    expect(apiMocks.post).toHaveBeenCalledWith(
      '/User',
      { email: 'player@example.test' },
      expect.objectContaining({ timeout: 10000 }),
    );
    expect(apiMocks.put).toHaveBeenCalledWith('/User/1', { firstName: 'Updated' });
    expect(apiMocks.delete).toHaveBeenCalledWith('/User/1');
    expect(apiMocks.get).toHaveBeenNthCalledWith(3, '/User/by-email/Player%2Btest%40example.test');
  });

  it('propagates account creation and lookup failures', async () => {
    const error = { response: { data: { message: 'Conflict' } } };
    apiMocks.post.mockRejectedValueOnce(error);
    apiMocks.get.mockRejectedValueOnce(error);
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined);

    await expect(createUser({ email: 'duplicate@example.test' })).rejects.toBe(error);
    await expect(getUserByEmail('duplicate@example.test')).rejects.toBe(error);

    expect(consoleError).toHaveBeenCalledTimes(2);
    consoleError.mockRestore();
  });
});
