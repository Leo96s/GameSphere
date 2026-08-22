import api from './api';

const userKey = 'user';

const persistSession = (session) => {
  if (!session?.user) {
    throw new Error('Erro no login: sessão inválida.');
  }

  localStorage.setItem(userKey, JSON.stringify(session.user));
  window.dispatchEvent(new Event('user-logged-in'));

  return session;
};

export const getCurrentUser = () => {
  const storedUser = localStorage.getItem(userKey);

  if (!storedUser || storedUser === 'undefined' || storedUser === 'null') {
    return null;
  }

  try {
    return JSON.parse(storedUser);
  } catch {
    return null;
  }
};

export const getCurrentRole = () => {
  const role = getCurrentUser()?.role;

  if (role === 1) {
    return 'Admin';
  }

  if (role === 0) {
    return 'User';
  }

  return role ?? null;
};

export const isAdmin = () => getCurrentRole() === 'Admin';

export const login = async (email, password) => {
  const response = await api.post('/User/login', { email, password });

  if (!response.data?.user) {
    throw new Error('Erro no login: sessão inválida.');
  }

  return persistSession(response.data);
};

export const social_login = async (idToken) => {
  const response = await api.post('/User/social-login', { idToken });

  if (!response.data?.user) {
    throw new Error('Erro no login: sessão inválida.');
  }

  return persistSession(response.data);
};

export const logout = () => {
  void api.post('/User/logout').catch(() => undefined);
  localStorage.removeItem(userKey);
  if (typeof window !== 'undefined') {
    window.dispatchEvent(new Event('user-logged-out'));
  }
};

export const sentResetCode = async (email) => {
  const response = await api.post('/User/send-reset-code', email, {
    headers: { 'Content-Type': 'application/json' },
  });

  if (response.data?.success === false) {
    throw new Error(response.data.message || 'Invalid email');
  }

  return response;
};

export const validateResetCodeRequest = async (email, resetCode) => {
  const response = await api.post(
    '/User/validate-reset-code',
    { email, resetCode },
    {
      headers: { 'Content-Type': 'application/json' },
    }
  );

  if (!response.data) {
    throw new Error(response.data?.message || 'Erro ao validar código de recuperação');
  }

  return response.data;
};

export const resetPassword = async (email, resetCode, newPassword) => {
  const response = await api.post(
    '/User/reset-password',
    { email, resetCode, newPassword },
    {
      headers: { 'Content-Type': 'application/json' },
    }
  );

  if (!response.data) {
    throw new Error('Erro ao enviar código de recuperação');
  }

  return response.data;
};
