<script setup lang="ts">
import { ref, useAttrs } from "vue"
import type { HTMLAttributes } from "vue"
import { useVModel } from "@vueuse/core"
import { Eye, EyeOff } from "lucide-vue-next"
import { Input } from "@/components/ui/input"
import { cn } from "@/lib/utils"

defineOptions({ inheritAttrs: false })

const props = defineProps<{
  defaultValue?: string | number
  modelValue?: string | number
  class?: HTMLAttributes["class"]
}>()

const emits = defineEmits<{
  (e: "update:modelValue", payload: string | number): void
}>()

const modelValue = useVModel(props, "modelValue", emits, {
  passive: true,
  defaultValue: props.defaultValue,
})

const attrs = useAttrs()
const isVisible = ref(false)
</script>

<template>
  <div class="relative">
    <Input
      v-model="modelValue"
      v-bind="attrs"
      :type="isVisible ? 'text' : 'password'"
      :class="cn('pr-9', props.class)"
    />
    <button
      type="button"
      class="absolute inset-y-0 right-0 flex items-center px-3 text-muted-foreground hover:text-foreground"
      :aria-label="isVisible ? 'Hide characters' : 'Show characters'"
      @click="isVisible = !isVisible"
    >
      <EyeOff v-if="isVisible" class="h-4 w-4" />
      <Eye v-else class="h-4 w-4" />
    </button>
  </div>
</template>
