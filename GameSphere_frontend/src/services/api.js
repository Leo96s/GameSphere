import axios from 'axios';

const api = axios.create({
    baseURL: 'https://localhost:7090/api', // URL base do backend
    timeout: 5000,
});

export default api;
