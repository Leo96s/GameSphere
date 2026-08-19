<template>
  <section class="min-h-full bg-muted/30 py-10 sm:py-14">
    <div class="mx-auto max-w-4xl px-4 sm:px-6">
      <header class="mb-8 max-w-2xl">
        <p class="mb-2 text-sm font-semibold text-primary">Quizzes</p>
        <h1 class="text-3xl font-bold tracking-tight sm:text-4xl">Escolhe um tema para começar</h1>
        <p class="mt-3 text-muted-foreground">Responde ao teu ritmo. Cada tentativa fica registada quando terminares.</p>
      </header>

      <p v-if="isLoading" class="rounded-xl border bg-card p-6 text-muted-foreground" role="status">A carregar quizzes…</p>

      <div v-else-if="errorMessage" class="rounded-xl border border-destructive/30 bg-destructive/5 p-6" role="alert">
        <p class="font-medium">Não foi possível carregar os quizzes.</p>
        <p class="mt-1 text-sm text-muted-foreground">{{ errorMessage }}</p>
        <button class="mt-4 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2" type="button" @click="loadQuizzes">
          Tentar novamente
        </button>
      </div>

      <div v-else-if="quizzes.length === 0" class="rounded-xl border bg-card p-6">
        <h2 class="font-semibold">Ainda não existem quizzes publicados</h2>
        <p class="mt-1 text-sm text-muted-foreground">Volta mais tarde para experimentar um novo tema.</p>
      </div>

      <ul v-else class="space-y-3" aria-label="Quizzes publicados">
        <li v-for="quiz in quizzes" :key="quiz.id">
          <router-link :to="{ name: 'quiz-play', params: { id: quiz.id } }" class="group flex items-center justify-between gap-4 rounded-xl border bg-card p-5 shadow-sm transition-colors hover:border-primary/50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2">
            <div>
              <h2 class="font-semibold group-hover:text-primary">{{ quiz.title }}</h2>
              <p class="mt-1 text-sm text-muted-foreground">{{ quiz.numberOfQuests }} {{ quiz.numberOfQuests === 1 ? 'pergunta' : 'perguntas' }} · {{ difficultyLabel(quiz.difficulty) }}</p>
            </div>
            <span class="shrink-0 text-sm font-medium text-primary">Começar<span aria-hidden="true"> →</span></span>
          </router-link>
        </li>
      </ul>
    </div>
  </section>
</template>

<script setup>
import { onMounted, ref } from 'vue';
import { listQuizzes } from '@/services/quizService';

const quizzes = ref([]);
const isLoading = ref(true);
const errorMessage = ref('');

const difficultyLabel = (difficulty) => ({
  0: 'Fácil',
  1: 'Médio',
  2: 'Difícil',
}[difficulty] ?? 'Sem dificuldade definida');

const loadQuizzes = async () => {
  isLoading.value = true;
  errorMessage.value = '';

  try {
    quizzes.value = await listQuizzes();
  } catch (error) {
    errorMessage.value = error?.response?.data?.message ?? 'Confirma a ligação e tenta novamente.';
  } finally {
    isLoading.value = false;
  }
};

onMounted(loadQuizzes);
</script>
