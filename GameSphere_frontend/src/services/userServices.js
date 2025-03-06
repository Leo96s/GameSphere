import api from './api';

// Função auxiliar para obter o token
const getAuthHeader = () => {
    const token = localStorage.getItem("token");  // Busca o token salvo
    return token ? { Authorization: `Bearer ${token}` } : {}; // Adiciona ao header se existir
};

export const getUsers = async () => {
    const response = await api.get('/User', { headers: getAuthHeader() });
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
        console.error("Erro ao criar usuário:", error.response?.data || error.message);
        throw error;  // Repassa o erro para ser tratado no frontend
    }
};

export const editUser = async (userId, userData) => {
    const response = await api.put(`/User/${userId}`, userData,  { headers: getAuthHeader() });
    return response.data;
};

export const deleteUser = async (userId) =>{
    const response = await api.delete(`/User/${userId}`,  { headers: getAuthHeader() });
    return response.data;
};

export const checkUserExist = async (uid, email) => {
    try {
        const encodedEmail = encodeURIComponent(email);  // 🔥 Codifica corretamente o email
        const response = await api.get(`/User/user-exist/${uid}/${encodedEmail}`,  { headers: getAuthHeader() });
        return response.data;
    } catch (error) {
        console.error("Erro ao verificar usuário:", error.response?.data || error.message);
        throw error;
    }
};

export const getUserByEmail = async (email) => {
    try{
        const encodedEmail = encodeURIComponent(email); 
        const response = await api.get(`/User/by-email/${encodedEmail}`,  { headers: getAuthHeader() });
        return response.data;

    }catch(error){
        console.error("Erro ao criar usuário:", error.response?.data || error.message);
        throw error;
    }
}
