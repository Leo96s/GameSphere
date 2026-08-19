<template>
  <section class="min-h-full bg-muted/30 py-10 sm:py-14">
    <div class="mx-auto max-w-5xl px-4 sm:px-6">
      <header class="flex flex-col gap-5 border-b pb-8 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p class="text-sm font-semibold text-primary">Administração</p>
          <h1 class="mt-1 text-3xl font-bold tracking-tight sm:text-4xl">Quizzes curados</h1>
          <p class="mt-3 max-w-2xl text-muted-foreground">Cria e revê o conteúdo antes de o tornar visível no catálogo.</p>
        </div>
        <button class="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2" type="button" @click="showCreateForm = !showCreateForm">
          {{ showCreateForm ? 'Fechar criação' : 'Criar quiz' }}
        </button>
      </header>

      <form v-if="showCreateForm" class="mt-6 rounded-xl border bg-card p-5 shadow-sm" @submit.prevent="handleCreate">
        <h2 class="font-semibold">Novo quiz</h2>
        <div class="mt-4 grid gap-4 sm:grid-cols-[1fr_12rem_auto] sm:items-end">
          <div>
            <label class="text-sm font-medium" for="new-quiz-title">Título</label>
            <input id="new-quiz-title" v-model.trim="newQuiz.title" class="mt-2 w-full rounded-md border bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring" maxlength="100" required>
          </div>
          <div>
            <label class="text-sm font-medium" for="new-quiz-difficulty">Dificuldade</label>
            <select id="new-quiz-difficulty" v-model.number="newQuiz.difficulty" class="mt-2 w-full rounded-md border bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
              <option :value="0">Fácil</option>
              <option :value="1">Médio</option>
              <option :value="2">Difícil</option>
            </select>
          </div>
          <button class="rounded-md border px-4 py-2 text-sm font-medium hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50" type="submit" :disabled="isCreating">
            {{ isCreating ? 'A criar…' : 'Criar rascunho' }}
          </button>
        </div>
      </form>

      <p v-if="isLoading" class="mt-6 rounded-xl border bg-card p-6 text-muted-foreground" role="status">A carregar quizzes…</p>

      <div v-else-if="errorMessage" class="mt-6 rounded-xl border border-destructive/30 bg-destructive/5 p-6" role="alert">
        <p class="font-medium">Não foi possível gerir os quizzes.</p>
        <p class="mt-1 text-sm text-muted-foreground">{{ errorMessage }}</p>
        <button class="mt-4 rounded-md border px-4 py-2 text-sm font-medium hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring" type="button" @click="loadQuizzes">Tentar novamente</button>
      </div>

      <div v-else-if="quizzes.length === 0" class="mt-6 rounded-xl border border-dashed bg-card p-8 text-center">
        <h2 class="font-semibold">Ainda não tens quizzes</h2>
        <p class="mt-2 text-sm text-muted-foreground">Cria o primeiro rascunho para começar a preparar perguntas.</p>
      </div>

      <ul v-else class="mt-6 space-y-3" aria-label="Quizzes administrados">
        <li v-for="quiz in quizzes" :key="quiz.id" class="rounded-xl border bg-card p-5 shadow-sm">
          <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div class="min-w-0">
              <div class="flex flex-wrap items-center gap-2">
                <h2 class="truncate text-lg font-semibold">{{ quiz.title }}</h2>
                <span :class="publicationClass(quiz.isPublished)" class="inline-flex items-center rounded-full px-2.5 py-1 text-xs font-semibold">
                  {{ publicationLabel(quiz.isPublished) }}
                </span>
              </div>
              <p class="mt-2 text-sm text-muted-foreground">{{ quiz.numberOfQuests }} {{ quiz.numberOfQuests === 1 ? 'pergunta' : 'perguntas' }} · {{ difficultyLabel(quiz.difficulty) }}</p>
            </div>
            <div class="flex flex-wrap gap-2">
              <router-link :to="{ name: 'admin-quiz-editor', params: { id: quiz.id } }" class="rounded-md border px-3 py-2 text-sm font-medium hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">Editar</router-link>
              <button class="rounded-md border px-3 py-2 text-sm font-medium hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50" type="button" :disabled="changingQuizId === quiz.id" @click="togglePublication(quiz)">
                {{ changingQuizId === quiz.id ? 'A guardar…' : quiz.isPublished ? 'Despublicar' : 'Publicar' }}
              </button>
              <button class="rounded-md border border-destructive/40 px-3 py-2 text-sm font-medium text-destructive hover:bg-destructive/5 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50" type="button" :disabled="changingQuizId === quiz.id" @click="handleDelete(quiz)">Apagar</button>
            </div>
          </div>
        </li>
      </ul>
    </div>
  </section>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import {
  createQuiz,
  deleteQuiz,
  getAdminErrorMessage,
  listAdminQuizzes,
  updateQuiz,
} from '@/services/adminQuizService';

const router = useRouter();
const quizzes = ref([]);
const isLoading = ref(true);
const isCreating = ref(false);
const showCreateForm = ref(false);
const changingQuizId = ref(null);
const errorMessage = ref('');
const newQuiz = reactive({ title: '', difficulty: 0 });

const difficultyLabel = (difficulty) => ({ 0: 'Fácil', 1: 'Médio', 2: 'Difícil' }[difficulty] ?? 'Sem dificuldade');
const publicationLabel = (isPublished) => (isPublished ? 'Publicado' : 'Rascunho');
const publicationClass = (isPublished) => isPublished
  ? 'bg-primary/15 text-primary ring-1 ring-primary/25'
  : 'bg-muted text-muted-foreground ring-1 ring-border';

const loadQuizzes = async () => {
  isLoading.value = true;
  errorMessage.value = '';
  try {
    quizzes.value = await listAdminQuizzes();
  } catch (error) {
    errorMessage.value = getAdminErrorMessage(error);
  } finally {
    isLoading.value = false;
  }
};

const handleCreate = async () => {
  if (!newQuiz.title || isCreating.value) return;
  isCreating.value = true;
  errorMessage.value = '';
  try {
    const quiz = await createQuiz({ title: newQuiz.title, difficulty: newQuiz.difficulty, isPublished: false });
    await router.push({ name: 'admin-quiz-editor', params: { id: quiz.id } });
  } catch (error) {
    errorMessage.value = getAdminErrorMessage(error);
  } finally {
    isCreating.value = false;
  }
};

const togglePublication = async (quiz) => {
  changingQuizId.value = quiz.id;
  errorMessage.value = '';
  try {
    const updated = await updateQuiz(quiz.id, {
      title: quiz.title,
      difficulty: quiz.difficulty,
      isPublished: !quiz.isPublished,
    });
    quizzes.value = quizzes.value.map((currentQuiz) => currentQuiz.id === updated.id ? updated : currentQuiz);
  } catch (error) {
    errorMessage.value = getAdminErrorMessage(error);
  } finally {
    changingQuizId.value = null;
  }
};

const handleDelete = async (quiz) => {
  if (!window.confirm(`Apagar o quiz “${quiz.title}”? Esta ação não pode ser desfeita.`)) return;
  changingQuizId.value = quiz.id;
  errorMessage.value = '';
  try {
    await deleteQuiz(quiz.id);
    quizzes.value = quizzes.value.filter((currentQuiz) => currentQuiz.id !== quiz.id);
  } catch (error) {
    errorMessage.value = getAdminErrorMessage(error);
  } finally {
    changingQuizId.value = null;
  }
};

onMounted(loadQuizzes);
</script>
