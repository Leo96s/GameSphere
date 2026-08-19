import { computed, ref } from 'vue';

export const useQuizAttempt = (quiz) => {
  const selectedAnswers = ref({});
  const questions = quiz?.questions ?? [];

  const selectAnswer = (questionId, selectedAnswer) => {
    selectedAnswers.value = {
      ...selectedAnswers.value,
      [questionId]: selectedAnswer,
    };
  };

  const isComplete = computed(() => questions.length > 0 && questions.every((question) => {
    const selectedAnswer = selectedAnswers.value[question.id];
    return typeof selectedAnswer === 'string' && selectedAnswer.trim().length > 0;
  }));

  const toRequest = () => ({
    answers: questions
      .filter((question) => Object.hasOwn(selectedAnswers.value, question.id))
      .map((question) => ({
        questionId: question.id,
        selectedAnswer: selectedAnswers.value[question.id],
      })),
  });

  const clear = () => {
    selectedAnswers.value = {};
  };

  return {
    clear,
    isComplete,
    selectAnswer,
    toRequest,
  };
};
