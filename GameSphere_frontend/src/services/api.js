import axios from 'axios';

const api = axios.create({
    baseURL: 'http://localhost:5095/api', // URL base do backend
    timeout: 15000,
});

export default api;
