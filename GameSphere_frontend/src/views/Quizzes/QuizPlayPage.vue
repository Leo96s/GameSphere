<template>
  <section class="min-h-full bg-muted/30 py-10 sm:py-14">
    <div class="mx-auto max-w-2xl px-4 sm:px-6">
      <p v-if="isLoading" class="rounded-xl border bg-card p-6 text-muted-foreground" role="status">A preparar o quiz…</p>

      <div v-else-if="errorMessage" class="rounded-xl border border-destructive/30 bg-destructive/5 p-6" role="alert">
        <p class="font-medium">Não foi possível abrir este quiz.</p>
        <p class="mt-1 text-sm text-muted-foreground">{{ errorMessage }}</p>
        <router-link :to="{ name: 'quiz-catalog' }" class="mt-4 inline-flex rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2">Voltar ao catálogo</router-link>
      </div>

      <div v-else-if="quiz && attempt" class="rounded-xl border bg-card p-6 shadow-sm sm:p-8">
        <header>
          <p class="text-sm font-semibold text-primary">{{ progressLabel }}</p>
          <div class="mt-3 h-2 overflow-hidden rounded-full bg-muted" aria-hidden="true">
            <div class="quiz-progress h-full rounded-full bg-primary" :style="{ width: `${progressPercent}%` }" />
          </div>
          <h1 class="mt-6 text-2xl font-bold tracking-tight sm:text-3xl">{{ quiz.title }}</h1>
        </header>

        <div v-if="quiz.questions.length === 0" class="mt-8 rounded-lg border border-dashed p-5" role="status">
          <h2 class="font-semibold">Este quiz ainda não tem perguntas</h2>
          <p class="mt-1 text-sm text-muted-foreground">Volta ao catálogo e escolhe outro quiz enquanto este é preparado.</p>
          <router-link :to="{ name: 'quiz-catalog' }" class="mt-4 inline-flex rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2">Voltar ao catálogo</router-link>
        </div>

        <form v-else class="mt-8" @submit.prevent="handleSubmit">
          <QuizQuestionList :questions="quiz.questions" :selected-answers="selectedAnswers" @select="selectAnswer" />

          <p v-if="submitError" class="mt-5 text-sm text-destructive" role="alert">{{ submitError }}</p>
          <button class="mt-8 inline-flex w-full items-center justify-center rounded-md bg-primary px-4 py-3 font-medium text-primary-foreground transition-colors hover:bg-primary/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50" type="submit" :disabled="!isAttemptComplete || isSubmitting">
            {{ isSubmitting ? 'A submeter…' : 'Terminar quiz' }}
          </button>
        </form>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, onMounted, ref, shallowRef } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useQuizAttempt } from '@/composables/useQuizAttempt';
import QuizQuestionList from '@/components/Quizzes/QuizQuestionList.vue';
import { getQuiz, getQuizPlayErrorMessage, isQuizUnavailableError, submitAttempt } from '@/services/quizService';

const route = useRoute();
const router = useRouter();
const quiz = ref(null);
const attempt = shallowRef(null);
const isLoading = ref(true);
const isSubmitting = ref(false);
const errorMessage = ref('');
const submitError = ref('');

const answeredQuestions = computed(() => attempt.value?.toRequest().answers.length ?? 0);
const isAttemptComplete = computed(() => attempt.value?.isComplete.value ?? false);
const progressPercent = computed(() => quiz.value?.questions.length
  ? Math.round(answeredQuestions.value * 100 / quiz.value.questions.length)
  : 0);
const progressLabel = computed(() => `Progresso: ${answeredQuestions.value} de ${quiz.value?.questions.length ?? 0}`);

const selectedAnswers = computed(() => Object.fromEntries(
  (attempt.value?.toRequest().answers ?? []).map((answer) => [answer.questionId, answer.selectedAnswer]),
));

const selectAnswer = (questionId, answer) => {
  attempt.value?.selectAnswer(questionId, answer);
};

const loadQuiz = async () => {
  isLoading.value = true;
  errorMessage.value = '';

  try {
    quiz.value = await getQuiz(route.params.id);
    attempt.value = useQuizAttempt(quiz.value);
  } catch (error) {
    errorMessage.value = getQuizPlayErrorMessage(error, 'Confirma a ligação e tenta novamente.');
  } finally {
    isLoading.value = false;
  }
};

const handleSubmit = async () => {
  if (!attempt.value?.isComplete.value || isSubmitting.value) return;

  isSubmitting.value = true;
  submitError.value = '';

  try {
    const result = await submitAttempt(quiz.value.id, attempt.value.toRequest());
    attempt.value.clear();
    await router.push({
      name: 'quiz-result',
      params: { id: quiz.value.id },
      state: { quizResult: result },
    });
  } catch (error) {
    if (isQuizUnavailableError(error)) {
      attempt.value?.clear();
      errorMessage.value = getQuizPlayErrorMessage(error);
      return;
    }
    submitError.value = getQuizPlayErrorMessage(error, 'Não foi possível submeter a tentativa. Tenta novamente.');
  } finally {
    isSubmitting.value = false;
  }
};

onMounted(loadQuiz);
</script>

<style scoped>
.quiz-progress {
  transition: width 180ms ease-out;
}

@media (prefers-reduced-motion: reduce) {
  .quiz-progress {
    transition: none;
  }
}
</style>
