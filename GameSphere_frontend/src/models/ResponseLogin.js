export default class Login {
  constructor({uid, email}) {
    this.id = 0; // O backend irá gerar um novo ID
    this.uid = uid;
    this.email = email;
  }

  /**
   * Valida os campos obrigatórios do modelo
   * @returns {Object} - Retorna um objeto com erros, se houver
   */
  validate() {
    const errors = {};
    if (!this.email) errors.email = "Email is required."
    return errors;
  }
}