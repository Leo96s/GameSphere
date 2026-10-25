// Traduz para PT-PT as mensagens conhecidas devolvidas pelo backend de administração.
// Mensagens desconhecidas não são traduzidas (devolve-se null) e quem chama usa o fallback.
export const ADMIN_MESSAGES = Object.freeze({
  // Normalização de quizzes e perguntas
  'A quiz title is required.': 'Indica o título do quiz.',
  'The quiz contains an invalid title or difficulty.': 'O título ou a dificuldade do quiz não são válidos.',
  'A quiz cannot contain more than 100 questions.': 'Um quiz não pode ter mais de 100 perguntas.',
  'A question, supported answer type, options and correct answer are required.':
    'Indica a pergunta, o formato, pelo menos duas opções e a resposta correta.',
  'A question or its options exceed the allowed size.':
    'A pergunta ou as opções excedem o tamanho permitido.',
  'Each question option must be unique and non-empty.': 'Cada opção tem de ser diferente e não pode estar vazia.',
  'The correct answer must be one of the question options.': 'A resposta correta tem de constar nas opções.',
  'The text contains unsupported control or invisible characters.':
    'O texto contém caracteres de controlo ou invisíveis que não são permitidos.',

  // Validação de publicação
  'The quiz title cannot exceed 100 characters.': 'O título do quiz não pode ter mais de 100 caracteres.',
  'The quiz difficulty is invalid.': 'A dificuldade do quiz não é válida.',
  'The quiz category is invalid.': 'A categoria do quiz não é válida.',
  'The quiz content key is invalid.': 'A chave de conteúdo do quiz não é válida.',
  'A quiz with this content key already exists.': 'Já existe um quiz com esta chave de conteúdo.',
  'The quiz content key cannot be changed.': 'A chave de conteúdo do quiz não pode ser alterada.',
  'A quiz must have at least one question to be published.':
    'Um quiz tem de ter pelo menos uma pergunta para ser publicado.',
  'A true or false question must have exactly two options.':
    'Uma pergunta de verdadeiro ou falso tem de ter exatamente duas opções.',

  // Handlers de administração
  'Quiz not found.': 'O quiz não foi encontrado.',
  'Question not found.': 'A pergunta não foi encontrada.',
  'Only draft or unpublished quizzes can be edited.':
    'Só é possível editar quizzes em rascunho ou retirados.',
  'A quiz with existing attempts cannot have its content changed.':
    'O conteúdo de um quiz que já tem tentativas não pode ser alterado.',
  'A quiz with existing attempts cannot be deleted.': 'Um quiz que já tem tentativas não pode ser apagado.',
  'Unpublish the quiz before deleting it.': 'Retira o quiz antes de o apagar.',
  'A valid quiz status is required.': 'Indica um estado de quiz válido.',
  'The authenticated administrator was not found.': 'Não foi possível identificar o administrador autenticado.',
  'The quiz could not be deleted because of related data.':
    'Não foi possível apagar o quiz porque tem dados associados.',

  // Respostas de modelo inválido
  'The request body is invalid.': 'Os dados enviados não são válidos.',
  'The request is invalid.': 'O pedido não é válido.',
});

const QUESTION_PREFIX = /^Question (\d+): ([\s\S]*)$/;
const STATUS_TRANSITION = /^Cannot change quiz status from (\w+) to (\w+)\.$/;

/**
 * @param {string} message mensagem original do backend
 * @param {(statusName: string) => string} statusLabel converte o nome do estado (ex.: "Draft") no rótulo PT-PT
 * @returns {string | null} tradução, ou null se a mensagem não for conhecida
 */
export const translateAdminMessage = (message, statusLabel = (name) => name) => {
  if (typeof message !== 'string') return null;

  const exact = ADMIN_MESSAGES[message];
  if (exact) return exact;

  const prefixed = QUESTION_PREFIX.exec(message);
  if (prefixed) {
    const rest = translateAdminMessage(prefixed[2], statusLabel);
    return rest ? `Pergunta ${prefixed[1]}: ${rest}` : null;
  }

  const transition = STATUS_TRANSITION.exec(message);
  if (transition) {
    return `Não é possível mudar o estado de «${statusLabel(transition[1])}» para «${statusLabel(transition[2])}».`;
  }

  return null;
};
