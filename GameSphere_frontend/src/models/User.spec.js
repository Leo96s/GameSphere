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

  it('accepts the first gender enum value as valid', () => {
    const user = new User({
      firstName: 'First',
      lastName: 'Gender',
      email: 'first@example.test',
      gender: 'Male',
      password: 'Test-password-123',
    });

    expect(user.validate()).toEqual({});
  });
});
