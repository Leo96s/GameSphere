// Correção local da pré-visualização: nada é enviado nem gravado.
// Compara com === (ordinal e exato), como o QuizAttemptScorer do backend.
export const gradeQuizPreview = (adminQuestions, selections) => {
  const questions = adminQuestions ?? [];
  const feedback = {};
  let correctCount = 0;

  for (const question of questions) {
    const selected = selections?.[question.id];
    if (selected === undefined) continue;

    const isCorrect = selected === question.correctAnswer;
    feedback[question.id] = { isCorrect, correctAnswer: question.correctAnswer };
    if (isCorrect) correctCount += 1;
  }

  return { feedback, correctCount, total: questions.length };
};
