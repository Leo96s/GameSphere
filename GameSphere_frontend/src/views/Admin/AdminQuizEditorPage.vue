<template>
  <section class="min-h-full bg-muted/30 py-10 sm:py-14">
    <div class="mx-auto max-w-4xl px-4 sm:px-6">
      <router-link :to="{ name: 'admin-quiz-list' }" class="text-sm font-medium text-primary underline-offset-4 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">← Voltar aos quizzes</router-link>

      <p v-if="isLoading" class="mt-6 rounded-xl border bg-card p-6 text-muted-foreground" role="status">A carregar editor…</p>

      <div v-else-if="errorMessage && !quiz" class="mt-6 rounded-xl border border-destructive/30 bg-destructive/5 p-6" role="alert">
        <p class="font-medium">Não foi possível abrir este quiz.</p>
        <p class="mt-1 text-sm text-muted-foreground">{{ errorMessage }}</p>
      </div>

      <template v-else-if="quiz">
        <header class="mt-6 rounded-xl border bg-card p-6 shadow-sm sm:p-8">
          <div class="flex flex-col gap-5 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <p class="text-sm font-semibold text-primary">Editor de conteúdo</p>
              <h1 class="mt-1 text-3xl font-bold tracking-tight">{{ quiz.title || 'Quiz sem título' }}</h1>
            </div>
            <span :class="publicationClass(quiz.isPublished)" class="inline-flex w-fit items-center rounded-full px-3 py-1.5 text-sm font-semibold">
              {{ quiz.isPublished ? 'Publicado' : 'Rascunho' }}
            </span>
          </div>

          <form class="mt-7 grid gap-5 sm:grid-cols-2" @submit.prevent="saveQuiz">
            <div class="sm:col-span-2">
              <label class="text-sm font-medium" for="quiz-title">Título</label>
              <input id="quiz-title" v-model.trim="quiz.title" class="mt-2 w-full rounded-md border bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring" maxlength="100" required>
            </div>
            <div>
              <label class="text-sm font-medium" for="quiz-difficulty">Dificuldade</label>
              <select id="quiz-difficulty" v-model.number="quiz.difficulty" class="mt-2 w-full rounded-md border bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
                <option :value="0">Fácil</option>
                <option :value="1">Médio</option>
                <option :value="2">Difícil</option>
              </select>
            </div>
            <label class="flex items-center gap-3 self-end rounded-md border p-3 text-sm font-medium">
              <input v-model="quiz.isPublished" class="size-4 accent-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring" type="checkbox">
              Publicar este quiz
            </label>
            <p v-if="quizError" class="sm:col-span-2 text-sm text-destructive" role="alert">{{ quizError }}</p>
            <div class="sm:col-span-2">
              <button class="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50" type="submit" :disabled="isSavingQuiz">
                {{ isSavingQuiz ? 'A guardar…' : 'Guardar quiz' }}
              </button>
            </div>
          </form>
        </header>

        <section class="mt-8" aria-labelledby="questions-heading">
          <div class="flex flex-col gap-3 border-b pb-5 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <p class="text-sm font-semibold text-primary">Conteúdo</p>
              <h2 id="questions-heading" class="mt-1 text-2xl font-bold tracking-tight">Perguntas</h2>
            </div>
            <button v-if="!newQuestion" class="rounded-md border px-4 py-2 text-sm font-medium hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring" type="button" @click="startQuestion">
              Adicionar pergunta
            </button>
          </div>

          <p v-if="questionError" class="mt-5 rounded-lg border border-destructive/30 bg-destructive/5 p-4 text-sm text-destructive" role="alert">{{ questionError }}</p>
          <p v-if="!newQuestion && quiz.questions.length === 0" class="mt-5 rounded-xl border border-dashed bg-card p-6 text-muted-foreground">Ainda não existem perguntas. Adiciona pelo menos uma antes de publicar.</p>

          <div class="mt-5 space-y-4">
            <AdminQuestionEditor
              v-if="newQuestion"
              :question="newQuestion"
              :index="quiz.questions.length"
              :is-saving="savingQuestionId === 'new'"
              @save="saveNewQuestion"
              @cancel="newQuestion = null"
            />
            <AdminQuestionEditor
              v-for="(question, index) in quiz.questions"
              :key="question.id"
              :question="question"
              :index="index"
              :is-saving="savingQuestionId === question.id"
              :is-deleting="deletingQuestionId === question.id"
              @save="saveQuestion(question.id, $event)"
              @delete="removeQuestion(question.id)"
            />
          </div>
        </section>
      </template>
    </div>
  </section>
</template>

<script setup>
import { onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import AdminQuestionEditor from '@/views/Admin/AdminQuestionEditor.vue';
import {
  createQuestion,
  deleteQuestion,
  getAdminErrorMessage,
  listAdminQuizzes,
  updateQuestion,
  updateQuiz,
} from '@/services/adminQuizService';

const route = useRoute();
const quiz = ref(null);
const isLoading = ref(true);
const isSavingQuiz = ref(false);
const savingQuestionId = ref(null);
const deletingQuestionId = ref(null);
const errorMessage = ref('');
const quizError = ref('');
const questionError = ref('');
const newQuestion = ref(null);

const publicationClass = (isPublished) => isPublished
  ? 'bg-primary/15 text-primary ring-1 ring-primary/25'
  : 'bg-muted text-muted-foreground ring-1 ring-border';

const loadQuiz = async () => {
  isLoading.value = true;
  errorMessage.value = '';
  try {
    const quizzes = await listAdminQuizzes();
    quiz.value = quizzes.find((candidate) => candidate.id === Number(route.params.id)) ?? null;
    if (!quiz.value) errorMessage.value = 'O quiz pedido não existe ou já foi apagado.';
    else quiz.value.questions ??= [];
  } catch (error) {
    errorMessage.value = getAdminErrorMessage(error);
  } finally {
    isLoading.value = false;
  }
};

const saveQuiz = async () => {
  if (!quiz.value || isSavingQuiz.value) return;
  isSavingQuiz.value = true;
  quizError.value = '';
  try {
    const updated = await updateQuiz(quiz.value.id, {
      title: quiz.value.title,
      difficulty: quiz.value.difficulty,
      isPublished: quiz.value.isPublished,
    });
    quiz.value = { ...quiz.value, ...updated, questions: updated.questions ?? quiz.value.questions };
  } catch (error) {
    quizError.value = getAdminErrorMessage(error);
  } finally {
    isSavingQuiz.value = false;
  }
};

const startQuestion = () => {
  newQuestion.value = { description: '', typeOfAnswer: 0, answers: ['', ''], correctAnswer: '' };
  questionError.value = '';
};

const saveNewQuestion = async (question) => {
  if (!quiz.value || savingQuestionId.value) return;
  savingQuestionId.value = 'new';
  questionError.value = '';
  try {
    const created = await createQuestion(quiz.value.id, question);
    quiz.value.questions.push(created);
    quiz.value.numberOfQuests = quiz.value.questions.length;
    newQuestion.value = null;
  } catch (error) {
    questionError.value = getAdminErrorMessage(error);
  } finally {
    savingQuestionId.value = null;
  }
};

const saveQuestion = async (questionId, question) => {
  if (!quiz.value || savingQuestionId.value) return;
  savingQuestionId.value = questionId;
  questionError.value = '';
  try {
    const updated = await updateQuestion(questionId, question);
    quiz.value.questions = quiz.value.questions.map((currentQuestion) => currentQuestion.id === questionId ? updated : currentQuestion);
  } catch (error) {
    questionError.value = getAdminErrorMessage(error);
  } finally {
    savingQuestionId.value = null;
  }
};

const removeQuestion = async (questionId) => {
  if (!quiz.value || deletingQuestionId.value) return;
  deletingQuestionId.value = questionId;
  questionError.value = '';
  try {
    await deleteQuestion(questionId);
    quiz.value.questions = quiz.value.questions.filter((question) => question.id !== questionId);
    quiz.value.numberOfQuests = quiz.value.questions.length;
  } catch (error) {
    questionError.value = getAdminErrorMessage(error);
  } finally {
    deletingQuestionId.value = null;
  }
};

onMounted(loadQuiz);
</script>
