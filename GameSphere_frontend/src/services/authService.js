import api from "./api"; // Configuração do Axios (ou use fetch)

export const login = async (email, password) => {
  const response = await api.post("/User/login", { email, password });
  return response; // Supondo que o backend retorna { token: "..." }
};
