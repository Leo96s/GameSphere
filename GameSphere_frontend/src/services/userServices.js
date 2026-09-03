import api from './api';

// Função auxiliar para obter o token
export const getUsers = async () => {
    const response = await api.get('/User');
    return response.data;
};

export const getUser = async (id) => {
    const response = await api.get(`/User/by-id/${id}`);
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
        console.error("Erro ao criar utilizador:", error.response?.data || error.message);
        throw error;  // Repassa o erro para ser tratado no frontend
    }
};

export const editUser = async (userId, userData) => {
    const response = await api.put(`/User/${userId}`, userData);
    return response.data;
};

export const deactivateAccount = async (userId, payload) => {
    const response = await api.post(`/User/${userId}/deactivate`, payload);
    return response.data;
};

export const getUserByEmail = async (email) => {
    try{
        const encodedEmail = encodeURIComponent(email);
        const response = await api.get(`/User/by-email/${encodedEmail}`);
        return response.data;

    }catch(error){
        console.error("Erro ao criar utilizador:", error.response?.data || error.message);
        throw error;
    }
}

export const changePassword = async (userId, payload) => {
    const response = await api.post(`/User/${userId}/password`, payload);
    return response.data;
};

export const requestEmailChange = async (userId, payload) => {
    const response = await api.post(`/User/${userId}/email/request`, payload);
    return response.data;
};

export const confirmEmailChange = async (userId, code) => {
    const response = await api.post(`/User/${userId}/email/confirm`, { code });
    return response.data;
};
