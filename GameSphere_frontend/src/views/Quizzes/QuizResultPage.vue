<template>
  <section class="min-h-full bg-muted/30 py-10 sm:py-14">
    <div class="mx-auto max-w-xl px-4 sm:px-6">
      <div v-if="result" class="rounded-xl border bg-card p-6 text-center shadow-sm sm:p-8">
        <p class="text-sm font-semibold text-primary">Tentativa concluída</p>
        <h1 class="mt-3 text-3xl font-bold tracking-tight">{{ percentageLabel }}</h1>
        <p class="mt-3 text-muted-foreground">Acertaste {{ result.correctAnswers }} de {{ result.totalQuestions }} {{ result.totalQuestions === 1 ? 'pergunta' : 'perguntas' }}.</p>
        <router-link :to="{ name: 'quiz-catalog' }" class="mt-8 inline-flex rounded-md bg-primary px-5 py-3 font-medium text-primary-foreground transition-colors hover:bg-primary/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2">Voltar ao catálogo</router-link>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';

const router = useRouter();
const result = ref(window.history.state?.quizResult ?? null);
const percentageLabel = computed(() => `${Number(result.value?.percentage ?? 0).toLocaleString('pt-PT', { maximumFractionDigits: 1 })}%`);

onMounted(() => {
  if (!result.value) {
    router.replace({ name: 'quiz-catalog' });
  }
});
</script>
