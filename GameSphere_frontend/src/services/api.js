import axios from 'axios';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5095/api';

const api = axios.create({
  baseURL: apiBaseUrl,
  timeout: 15000,
  withCredentials: true,
});

export default api;
