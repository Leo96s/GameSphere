import { describe, expect, it } from 'vitest';
import * as z from 'zod';
import { toTypedSchema } from './useAppForm';

const resetPasswordSchema = z.object({
  resetCode: z.string().min(6, 'The code must be 6 characters'),
  password: z.string().min(8, 'The password must be at least 8 characters'),
  confirmPassword: z.string().min(1, 'Confirm your password'),
}).refine((data) => data.password === data.confirmPassword, {
  message: 'Passwords do not match',
  path: ['confirmPassword'],
});

describe('toTypedSchema', () => {
  it('devolve o valor validado e nenhum erro quando o formulário é válido', async () => {
    const values = { resetCode: '123456', password: 'password1', confirmPassword: 'password1' };

    await expect(toTypedSchema(resetPasswordSchema).parse(values)).resolves.toEqual({
      value: values,
      errors: [],
    });
  });

  it('agrupa as mensagens por campo', async () => {
    const schema = z.object({
      email: z.string().min(1, 'Email is required').email('Invalid email'),
    });

    const { value, errors } = await toTypedSchema(schema).parse({ email: '' });

    expect(value).toBeUndefined();
    expect(errors).toEqual([{ path: 'email', errors: ['Email is required', 'Invalid email'] }]);
  });

  it('associa o erro de um refine ao caminho indicado', async () => {
    const { errors } = await toTypedSchema(resetPasswordSchema).parse({
      resetCode: '123456',
      password: 'password1',
      confirmPassword: 'password2',
    });

    expect(errors).toEqual([{ path: 'confirmPassword', errors: ['Passwords do not match'] }]);
  });

  it('usa a sintaxe de caminhos do vee-validate para arrays e objectos aninhados', async () => {
    const schema = z.object({
      players: z.array(z.object({ name: z.string().min(1, 'Name is required') })),
    });

    const { errors } = await toTypedSchema(schema).parse({ players: [{ name: 'Ana' }, { name: '' }] });

    expect(errors).toEqual([{ path: 'players[1].name', errors: ['Name is required'] }]);
  });

  it('valida campos booleanos refinados', async () => {
    const schema = z.object({
      acceptedPolicy: z.boolean().refine((v) => v, 'You must accept the terms'),
    });

    const { errors } = await toTypedSchema(schema).parse({ acceptedPolicy: false });

    expect(errors).toEqual([{ path: 'acceptedPolicy', errors: ['You must accept the terms'] }]);
  });
});
