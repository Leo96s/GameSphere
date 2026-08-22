import { describe, expect, it } from 'vitest';
import User from './User';

describe('User model', () => {
  it('preserves the external provider uid for social registration', () => {
    const user = new User({
      firstName: 'Social',
      lastName: 'User',
      email: 'social@example.test',
      gender: 'Other',
      password: 'Generated-password-123',
      uid: 'firebase-user-id',
    });

    expect(user.uid).toBe('firebase-user-id');
  });
});
