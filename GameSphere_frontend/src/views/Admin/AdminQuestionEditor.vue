<script>
export const validateQuestion = (question) => {
  const answers = question.answers.map((answer) => answer.trim()).filter(Boolean);
  const correctAnswer = question.correctAnswer?.trim();

  if (answers.length < 2) {
    return 'Indica pelo menos duas opções.';
  }

  if (new Set(answers).size !== answers.length) {
    return 'Cada opção tem de ser diferente.';
  }

  if (!answers.includes(correctAnswer)) {
    return 'A resposta correta tem de constar nas opções.';
  }

  if (!question.description?.trim()) {
    return 'Escreve a pergunta antes de a guardar.';
  }

  return '';
};
</script>

<template>
  <article class="rounded-xl border bg-card p-5 shadow-sm">
    <div class="flex items-start justify-between gap-4">
      <div>
        <p class="text-xs font-semibold uppercase tracking-[0.16em] text-primary">Pergunta {{ index + 1 }}</p>
        <h3 class="mt-1 font-semibold">{{ question.id ? 'Editar pergunta' : 'Nova pergunta' }}</h3>
      </div>
      <button
        v-if="question.id"
        class="text-sm font-medium text-destructive underline-offset-4 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
        type="button"
        :disabled="isDeleting"
        @click="$emit('delete')"
      >
        {{ isDeleting ? 'A apagar…' : 'Apagar' }}
      </button>
    </div>

    <form class="mt-5 space-y-5" @submit.prevent="submit">
      <div>
        <label class="text-sm font-medium" :for="`question-${question.id ?? 'new'}-description`">Pergunta</label>
        <textarea
          :id="`question-${question.id ?? 'new'}-description`"
          v-model="draft.description"
          class="mt-2 min-h-24 w-full rounded-md border bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          required
        />
      </div>

      <div>
        <label class="text-sm font-medium" :for="`question-${question.id ?? 'new'}-type`">Formato</label>
        <select
          :id="`question-${question.id ?? 'new'}-type`"
          v-model.number="draft.typeOfAnswer"
          class="mt-2 w-full rounded-md border bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
        >
          <option :value="0">Múltipla escolha</option>
          <option :value="1">Verdadeiro ou falso</option>
        </select>
      </div>

      <fieldset>
        <legend class="text-sm font-medium">Opções</legend>
        <p class="mt-1 text-sm text-muted-foreground">Escolhe uma das opções como resposta correta.</p>
        <div class="mt-3 space-y-2">
          <div v-for="(_, answerIndex) in draft.answers" :key="answerIndex" class="flex gap-2">
            <label class="sr-only" :for="`question-${question.id ?? 'new'}-answer-${answerIndex}`">Opção {{ answerIndex + 1 }}</label>
            <input
              :id="`question-${question.id ?? 'new'}-answer-${answerIndex}`"
              v-model="draft.answers[answerIndex]"
              class="min-w-0 flex-1 rounded-md border bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              :placeholder="`Opção ${answerIndex + 1}`"
              required
            >
            <button
              v-if="draft.answers.length > 2"
              class="rounded-md border px-3 text-sm font-medium hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              type="button"
              :aria-label="`Remover opção ${answerIndex + 1}`"
              @click="removeAnswer(answerIndex)"
            >
              Remover
            </button>
          </div>
        </div>
        <button class="mt-3 text-sm font-medium text-primary underline-offset-4 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring" type="button" @click="addAnswer">
          Adicionar opção
        </button>
      </fieldset>

      <div>
        <label class="text-sm font-medium" :for="`question-${question.id ?? 'new'}-correct`">Resposta correta</label>
        <select
          :id="`question-${question.id ?? 'new'}-correct`"
          v-model="draft.correctAnswer"
          class="mt-2 w-full rounded-md border bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
        >
          <option value="" disabled>Seleciona a resposta correta</option>
          <option v-for="answer in availableAnswers" :key="answer" :value="answer">{{ answer }}</option>
        </select>
      </div>

      <p v-if="validationError" class="text-sm text-destructive" role="alert">{{ validationError }}</p>

      <div class="flex flex-wrap gap-3">
        <button class="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50" type="submit" :disabled="isSaving">
          {{ isSaving ? 'A guardar…' : 'Guardar pergunta' }}
        </button>
        <button v-if="!question.id" class="rounded-md border px-4 py-2 text-sm font-medium hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring" type="button" @click="$emit('cancel')">
          Cancelar
        </button>
      </div>
    </form>
  </article>
</template>

<script setup>
import { computed, reactive, ref, watch } from 'vue';

const props = defineProps({
  question: { type: Object, required: true },
  index: { type: Number, required: true },
  isSaving: { type: Boolean, default: false },
  isDeleting: { type: Boolean, default: false },
});

const emit = defineEmits(['save', 'delete', 'cancel']);
const validationError = ref('');
const draft = reactive({
  description: '',
  typeOfAnswer: 0,
  answers: ['', ''],
  correctAnswer: '',
});

const hydrate = (question) => {
  draft.description = question.description ?? '';
  draft.typeOfAnswer = question.typeOfAnswer ?? 0;
  draft.answers = question.answers?.length ? [...question.answers] : ['', ''];
  draft.correctAnswer = question.correctAnswer ?? '';
  validationError.value = '';
};

watch(() => props.question, hydrate, { deep: true, immediate: true });

const availableAnswers = computed(() => draft.answers.map((answer) => answer.trim()).filter(Boolean));

const addAnswer = () => draft.answers.push('');

const removeAnswer = (index) => {
  const removedAnswer = draft.answers[index];
  draft.answers.splice(index, 1);
  if (draft.correctAnswer === removedAnswer) draft.correctAnswer = '';
};

const submit = () => {
  const question = {
    description: draft.description.trim(),
    typeOfAnswer: draft.typeOfAnswer,
    answers: draft.answers.map((answer) => answer.trim()).filter(Boolean),
    correctAnswer: draft.correctAnswer.trim(),
  };
  validationError.value = validateQuestion(question);

  if (!validationError.value) emit('save', question);
};
</script>
