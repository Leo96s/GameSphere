import api from './api';

export const getUsers = async () => {
    const response = await api.get('/User');
    return response.data;
};

export const getUser = async (userData) => {
    const response = await api.get('/User', userData);
    return response.data;
};

export const createUser = async (userData) => {
    const response = await api.post("/User", userData);
    return response.data;
};

export const editUser = async (userId, userData) => {
    const response = await api.put(`/User/${userId}`, userData);
    return response.data;
};

export const deleteUser = async (userId) =>{
    const response = await api.delete(`/User/${userId}`);
    return response.data;
};

export const loginUser = async (userData) => {
    const response = await api.post("/User/login", userData);
    return response.data;
}
