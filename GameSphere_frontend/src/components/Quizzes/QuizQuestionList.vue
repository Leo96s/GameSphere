<template>
  <fieldset class="space-y-5">
    <legend class="sr-only">Perguntas do quiz</legend>
    <article v-for="(question, index) in questions" :key="question.id" class="border-t pt-5 first:border-t-0 first:pt-0">
      <p class="text-sm font-medium text-muted-foreground">Pergunta {{ index + 1 }}</p>
      <h2 class="mt-1 text-lg font-semibold">{{ question.description }}</h2>
      <div class="mt-4 space-y-2" role="radiogroup" :aria-label="`Respostas para a pergunta ${index + 1}`">
        <label v-for="answer in question.answers" :key="answer" class="flex cursor-pointer items-center gap-3 rounded-lg border p-3 text-sm transition-colors has-[:checked]:border-primary has-[:checked]:bg-primary/5">
          <input class="size-4 accent-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring" type="radio" :name="`question-${question.id}`" :value="answer" :checked="selectedAnswers[question.id] === answer" :disabled="disabled" @change="emit('select', question.id, answer)">
          <span>{{ answer }}</span>
        </label>
      </div>
      <div v-if="feedback?.[question.id]" class="mt-3 text-sm" role="status">
        <p :class="feedback[question.id].isCorrect ? 'font-semibold text-primary' : 'font-semibold text-destructive'">
          {{ feedback[question.id].isCorrect ? 'Certa' : 'Errada' }}
        </p>
        <p class="text-muted-foreground">Resposta correta: {{ feedback[question.id].correctAnswer }}</p>
      </div>
    </article>
  </fieldset>
</template>

<script setup>
defineProps({
  questions: { type: Array, required: true },
  selectedAnswers: { type: Object, default: () => ({}) },
  disabled: { type: Boolean, default: false },
  feedback: { type: Object, default: null },
});

const emit = defineEmits(['select']);
</script>
