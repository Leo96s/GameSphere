// UserModel.js
import {Gender} from "@/enums/Gender";

export default class User {
  constructor({ firstName, lastName, email, gender, password, uid = "" }) {
    this.id = 0; // O backend irá gerar um novo ID
    this.uid = uid;
    this.password = password;
    this.firstName = firstName;
    this.lastName = lastName;
    this.email = email;
    this.gender = Number(Gender[gender] ?? Gender.Other); // Converte para o formato esperado
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
    if (this.gender === undefined || this.gender === null) errors.gender = "Gender is required.";
    if (!this.password) errors.password = "Password is required.";
    return errors;
  }
}

