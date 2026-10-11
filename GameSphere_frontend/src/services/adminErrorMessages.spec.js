import { describe, expect, it } from 'vitest';
import { ADMIN_MESSAGES, translateAdminMessage } from './adminErrorMessages';

describe('adminErrorMessages', () => {
  it('tem uma tradução não vazia para cada mensagem conhecida', () => {
    const entries = Object.entries(ADMIN_MESSAGES);

    expect(entries.length).toBeGreaterThan(0);
    entries.forEach(([original, translation]) => {
      expect(original.trim()).not.toBe('');
      expect(translation.trim()).not.toBe('');
      expect(translateAdminMessage(original)).toBe(translation);
    });
  });

  it('traduz o prefixo de pergunta e o resto da mensagem', () => {
    expect(translateAdminMessage('Question 4: A quiz title is required.'))
      .toBe('Pergunta 4: Indica o título do quiz.');
    expect(translateAdminMessage('Question 4: Unknown.')).toBeNull();
  });

  it('traduz a mensagem de caracteres não suportados, com e sem prefixo de pergunta', () => {
    const message = 'The text contains unsupported control or invisible characters.';
    const translated = translateAdminMessage(message);

    expect(translated).toEqual(expect.any(String));
    expect(translateAdminMessage(`Question 2: ${message}`)).toBe(`Pergunta 2: ${translated}`);
  });

  it('traduz as transições de estado com os rótulos fornecidos', () => {
    const labels = { Draft: 'Rascunho', Archived: 'Arquivado' };

    expect(translateAdminMessage('Cannot change quiz status from Draft to Archived.', (name) => labels[name]))
      .toBe('Não é possível mudar o estado de «Rascunho» para «Arquivado».');
    expect(translateAdminMessage('Cannot change quiz status from Draft to Archived.'))
      .toBe('Não é possível mudar o estado de «Draft» para «Archived».');
  });

  it('devolve null para mensagens desconhecidas ou inválidas', () => {
    expect(translateAdminMessage('Something unexpected.')).toBeNull();
    expect(translateAdminMessage(undefined)).toBeNull();
    expect(translateAdminMessage(42)).toBeNull();
  });
});
