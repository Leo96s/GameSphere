// UserModel.js
import {Gender} from "@/enums/Gender";

export default class User {
  constructor({firstName, lastName, email, gender, password }) {
    this.id = 0; // O backend irá gerar um novo ID
    this.hashedPassword = password;
    this.firstName = firstName;
    this.lastName = lastName;
    this.email = email;
    this.gender = Gender[gender]; // Converte para o formato esperado
    this.image = ""; // Pode ser preenchido com um avatar padrão
    this.level = 1; // Nível inicial do usuário
    this.totalPoints = 0; // Pontos iniciais
    this.isActive = true; // Define o utilizador como ativo
    this.token = ""; // O backend pode preencher este campo após o login
    this.tokenExpDate = new Date().toISOString(); // Define um valor padrão
    this.registrationDate = new Date().toISOString(); // Data atual do registro
  }

  /**
   * Valida os campos obrigatórios do modelo
   * @returns {Object} - Retorna um objeto com erros, se houver
   */
  validate() {
    const errors = {};
    if (!this.firstName) errors.firstName = "First name is required.";
    if (!this.lastName) errors.lastName = "Last name is required.";
    if (!this.email) errors.email = "Email is required.";
    if (!this.gender) errors.gender = "Gender is required.";
    if (!this.hashedPassword) errors.hashedPassword = "Password is required.";
    return errors;
  }
}

