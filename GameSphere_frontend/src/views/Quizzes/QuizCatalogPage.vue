<template>
  <section class="min-h-full bg-muted/30 py-10 sm:py-14">
    <div class="mx-auto max-w-4xl px-4 sm:px-6">
      <header class="mb-8 max-w-2xl">
        <p class="mb-2 text-sm font-semibold text-primary">Quizzes</p>
        <h1 class="text-3xl font-bold tracking-tight sm:text-4xl">Escolhe um tema para começar</h1>
        <p class="mt-3 text-muted-foreground">Responde ao teu ritmo. Cada tentativa fica registada quando terminares.</p>
      </header>

      <form class="mb-6 grid gap-4 sm:grid-cols-2" aria-label="Filtros do catálogo" @submit.prevent>
        <div>
          <label class="text-sm font-medium" for="catalog-category">Categoria</label>
          <select id="catalog-category" v-model="categoryValue" class="mt-2 w-full rounded-md border bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
            <option value="">Todas</option>
            <option v-for="option in QUIZ_CATEGORIES" :key="option.value" :value="String(option.value)">{{ option.label }}</option>
          </select>
        </div>
        <div>
          <label class="text-sm font-medium" for="catalog-difficulty">Dificuldade</label>
          <select id="catalog-difficulty" v-model="difficultyValue" class="mt-2 w-full rounded-md border bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
            <option value="">Todas</option>
            <option v-for="option in DIFFICULTIES" :key="option.value" :value="String(option.value)">{{ option.label }}</option>
          </select>
        </div>
      </form>

      <p v-if="isLoading" class="rounded-xl border bg-card p-6 text-muted-foreground" role="status">A carregar quizzes…</p>

      <div v-else-if="errorMessage" class="rounded-xl border border-destructive/30 bg-destructive/5 p-6" role="alert">
        <p class="font-medium">Não foi possível carregar os quizzes.</p>
        <p class="mt-1 text-sm text-muted-foreground">{{ errorMessage }}</p>
        <button class="mt-4 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2" type="button" @click="loadQuizzes">
          Tentar novamente
        </button>
      </div>

      <div v-else-if="quizzes.length === 0 && filtersActive" class="rounded-xl border bg-card p-6">
        <h2 class="font-semibold">Não há quizzes para estes filtros.</h2>
        <button class="mt-4 rounded-md border px-4 py-2 text-sm font-medium hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2" type="button" @click="clearFilters">
          Limpar filtros
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
              <p class="mt-1 text-sm text-muted-foreground">{{ quizCategoryLabel(quiz.category) }} · {{ quiz.numberOfQuests }} {{ quiz.numberOfQuests === 1 ? 'pergunta' : 'perguntas' }} · {{ difficultyLabel(quiz.difficulty) }}</p>
            </div>
            <span class="shrink-0 text-sm font-medium text-primary">Começar<span aria-hidden="true"> →</span></span>
          </router-link>
        </li>
      </ul>
    </div>
  </section>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { DIFFICULTIES, QUIZ_CATEGORIES, difficultyLabel, quizCategoryLabel } from '@/enums/quiz';
import { listQuizzes } from '@/services/quizService';
import { filtersFromQuery, hasActiveFilters, queryFromFilters, sameFilters } from './catalogFilters';

const route = useRoute();
const router = useRouter();

const quizzes = ref([]);
const isLoading = ref(true);
const errorMessage = ref('');

// The URL is the source of truth: invalid query values are ignored, and back/reload restore the filters.
const filters = computed(() => filtersFromQuery(route.query));
const filtersActive = computed(() => hasActiveFilters(filters.value));

const toSelectValue = (value) => (value === null ? '' : String(value));
const toFilterValue = (value) => (value === '' ? null : Number(value));

const categoryValue = computed({
  get: () => toSelectValue(filters.value.category),
  set: (value) => updateFilters({ ...filters.value, category: toFilterValue(value) }),
});
const difficultyValue = computed({
  get: () => toSelectValue(filters.value.difficulty),
  set: (value) => updateFilters({ ...filters.value, difficulty: toFilterValue(value) }),
});

const updateFilters = (next) => router.push({ name: 'quiz-catalog', query: queryFromFilters(next) });
const clearFilters = () => updateFilters({ category: null, difficulty: null });

let latestRequest = 0;
const loadQuizzes = async () => {
  const request = ++latestRequest;
  isLoading.value = true;
  errorMessage.value = '';

  try {
    const result = await listQuizzes(filters.value);
    if (request === latestRequest) quizzes.value = result;
  } catch (error) {
    if (request === latestRequest) {
      errorMessage.value = error?.response?.data?.message ?? 'Confirma a ligação e tenta novamente.';
    }
  } finally {
    if (request === latestRequest) isLoading.value = false;
  }
};

// Ignore query changes caused by leaving the page (the component is about to unmount).
// Reload only when the effective filter values change, not when the query changes to an equivalent one.
watch(filters, (next, previous) => {
  if (route.name === 'quiz-catalog' && !sameFilters(next, previous)) loadQuizzes();
});
onMounted(loadQuizzes);
</script>
