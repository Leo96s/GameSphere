import api from "./api"; // Configuração do Axios (ou use fetch)

export const login = async (email, password) => {
  try {
    const response = await api.post("/User/login", { email, password });

    if (response.data && response.data.token) {
      // Se o backend retornou um token
      localStorage.setItem("token", response.data.token); // Salva o token
      return response.data; // Retorna os dados do usuário + token
    } else {
      throw new Error("Erro no login: Nenhum token recebido.");
    }
  } catch (error) {
    console.error(
      "Erro ao fazer login:",
      error.response?.data || error.message
    );
    throw error;
  } // Supondo que o backend retorna { token: "..." }
};

export const social_login = async (uid, email) => {
  try {
    const response = await api.post(`/User/social-login/${uid}/${email}`, { uid, email });

    if (response.data && response.data.token) {
      // Se o backend retornou um token
      localStorage.setItem("token", response.data.token); // Salva o token
      return response.data; // Retorna os dados do usuário + token
    } else {
      throw new Error("Erro no login: Nenhum token recebido.");
    }
  } catch (error) {
    console.error(
      "Erro ao fazer login:",
      error.response?.data || error.message
    );
    throw error;
  } // Supondo que o backend retorna { token: "..." }
};

export const logout = () => {
    localStorage.removeItem("token");
    localStorage.removeItem("user");
};

export const sentResetCode = async (email) => {
  try {
    const response = await api.post("/User/sent-reset-code", { email });

    if(response){
      return response;
    }else{
      throw new Error("Erro ao enviar código de recuperação");
    }
  }catch (error) {
    console.error(
      "Erro ao fazer login:",
      error.response?.data || error.message
    );
    throw error;
  }
};

export const resetPassword = async (email, resetCode, newPassword) =>{
  try {
    const response = await api.post("/User/reset-password", { email, resetCode, newPassword });

    if(response){
      return response;
    }else{
      throw new Error("Erro ao enviar código de recuperação");
    }
  }catch (error) {
    console.error(
      "Erro ao fazer login:",
      error.response?.data || error.message
    );
    throw error;
  }
}
