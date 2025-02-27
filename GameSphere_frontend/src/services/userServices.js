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
    try {
        const response = await api.post("/User", userData, {  // Remova JSON.stringify()
            headers: { 
                "Content-Type": "application/json",
                "Accept": "application/json"
            },
            timeout: 10000 // Aumente o timeout para 10s (caso o backend seja lento)
        });
        
        return response.data;
    } catch (error) {
        console.error("Erro ao criar usuário:", error.response?.data || error.message);
        throw error;  // Repassa o erro para ser tratado no frontend
    }
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
