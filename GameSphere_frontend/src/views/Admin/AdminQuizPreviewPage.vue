<template>
  <section class="min-h-full bg-muted/30 py-10 sm:py-14">
    <div class="mx-auto max-w-2xl px-4 sm:px-6">
      <div class="flex flex-wrap gap-4 text-sm font-medium">
        <router-link :to="{ name: 'admin-quiz-editor', params: { id: route.params.id } }" class="text-primary underline-offset-4 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">← Voltar ao editor</router-link>
        <router-link :to="{ name: 'admin-quiz-list' }" class="text-primary underline-offset-4 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">Voltar à lista</router-link>
      </div>

      <p v-if="isLoading" class="mt-6 rounded-xl border bg-card p-6 text-muted-foreground" role="status">A preparar a pré-visualização…</p>

      <div v-else-if="errorMessage && !quiz" class="mt-6 rounded-xl border border-destructive/30 bg-destructive/5 p-6" role="alert">
        <p class="font-medium">Não foi possível abrir a pré-visualização.</p>
        <p class="mt-1 text-sm text-muted-foreground">{{ errorMessage }}</p>
      </div>

      <div v-else-if="quiz && attempt" class="mt-6 rounded-xl border bg-card p-6 shadow-sm sm:p-8">
        <p class="rounded-lg border bg-muted/50 p-3 text-sm text-muted-foreground" role="status">Pré-visualização: as respostas não são gravadas.</p>

        <header class="mt-6">
          <h1 class="text-2xl font-bold tracking-tight sm:text-3xl">{{ quiz.title }}</h1>
          <div class="mt-3 flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
            <span>{{ quizCategoryLabel(quiz.category) }}</span>
            <span aria-hidden="true">·</span>
            <span>{{ difficultyLabel(quiz.difficulty) }}</span>
            <span aria-hidden="true">·</span>
            <span :class="statusClass(status)" class="inline-flex items-center rounded-full px-2.5 py-1 text-xs font-semibold">{{ quizStatusLabel(status) }}</span>
          </div>
        </header>

        <div class="mt-5 flex flex-wrap gap-2">
          <button
            v-for="action in quizStatusActions(status)"
            :key="action.status"
            class="rounded-md border px-3 py-2 text-sm font-medium hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
            type="button"
            :disabled="isChangingStatus"
            @click="handleStatusChange(action.status)"
          >
            {{ isChangingStatus ? 'A guardar…' : action.label }}
          </button>
        </div>
        <p v-if="actionError" class="mt-3 text-sm text-destructive" role="alert">{{ actionError }}</p>

        <div v-if="quiz.questions.length === 0" class="mt-8 rounded-lg border border-dashed p-5" role="status">
          <p class="text-sm text-muted-foreground">Este quiz ainda não tem perguntas.</p>
        </div>

        <div v-else class="mt-8">
          <QuizQuestionList :questions="quiz.questions" :selected-answers="selectedAnswers" :feedback="feedback" @select="selectAnswer" />

          <p v-if="result" class="mt-6 text-sm font-semibold" role="status">{{ result.correctCount }} de {{ result.total }} certas</p>

          <div class="mt-6 flex flex-wrap gap-3">
            <button class="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50" type="button" :disabled="!isAttemptComplete" @click="checkAnswers">Verificar respostas</button>
            <button class="rounded-md border px-4 py-2 text-sm font-medium hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring" type="button" @click="restart">Recomeçar</button>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, onMounted, ref, shallowRef } from 'vue';
import { useRoute } from 'vue-router';
import QuizQuestionList from '@/components/Quizzes/QuizQuestionList.vue';
import { useQuizAttempt } from '@/composables/useQuizAttempt';
import { difficultyLabel, quizCategoryLabel } from '@/enums/quiz';
import {
  changeQuizStatus,
  getAdminErrorMessage,
  getAdminQuiz,
  getQuizPreview,
  QUIZ_STATUS,
  quizStatusActions,
  quizStatusLabel,
} from '@/services/adminQuizService';
import { gradeQuizPreview } from '@/services/quizPreview';

const route = useRoute();
const quiz = ref(null);
const adminQuiz = ref(null);
const attempt = shallowRef(null);
const status = ref(null);
const result = ref(null);
const isLoading = ref(true);
const isChangingStatus = ref(false);
const errorMessage = ref('');
const actionError = ref('');

const feedback = computed(() => result.value?.feedback ?? null);
const isAttemptComplete = computed(() => attempt.value?.isComplete.value ?? false);
const selectedAnswers = computed(() => Object.fromEntries(
  (attempt.value?.toRequest().answers ?? []).map((answer) => [answer.questionId, answer.selectedAnswer]),
));

const statusClass = (value) => value === QUIZ_STATUS.PUBLISHED
  ? 'bg-primary/15 text-primary ring-1 ring-primary/25'
  : 'bg-muted text-muted-foreground ring-1 ring-border';

const selectAnswer = (questionId, answer) => {
  attempt.value?.selectAnswer(questionId, answer);
  result.value = null;
};

const checkAnswers = () => {
  if (!isAttemptComplete.value) return;
  result.value = gradeQuizPreview(adminQuiz.value?.questions ?? [], selectedAnswers.value);
};

const restart = () => {
  attempt.value?.clear();
  result.value = null;
};

const loadPreview = async () => {
  isLoading.value = true;
  errorMessage.value = '';

  try {
    const [preview, detail] = await Promise.all([
      getQuizPreview(route.params.id),
      getAdminQuiz(route.params.id),
    ]);
    quiz.value = preview;
    adminQuiz.value = detail;
    status.value = detail.status;
    attempt.value = useQuizAttempt(preview);
  } catch (error) {
    errorMessage.value = getAdminErrorMessage(error);
  } finally {
    isLoading.value = false;
  }
};

const handleStatusChange = async (nextStatus) => {
  if (isChangingStatus.value) return;

  isChangingStatus.value = true;
  actionError.value = '';

  try {
    const updated = await changeQuizStatus(route.params.id, nextStatus);
    status.value = updated?.status ?? nextStatus;
  } catch (error) {
    actionError.value = getAdminErrorMessage(error);
  } finally {
    isChangingStatus.value = false;
  }
};

onMounted(loadPreview);
</script>
