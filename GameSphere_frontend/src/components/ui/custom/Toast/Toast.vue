<template>
    <transition name="fade">
      <div
        v-if="visible"
        class="
          fixed top-6 left-1/2 -translate-x-1/2 z-50
          min-w-[280px] max-w-sm
          rounded-lg border bg-gray-800
          text-popover-foreground
          shadow-lg
          px-4 py-3
        "
        :class="variantClasses"
      >
        <p class="font-medium text-white">{{ title }}</p>
        <p v-if="description" class="text-sm text-muted-foreground mt-1">
          {{ description }}
        </p>
      </div>
    </transition>
  </template>

  <script setup lang="ts">
  import { ref, computed, defineExpose } from 'vue'

  type ToastVariant = 'success' | 'error'

  const visible = ref(false)
  const title = ref('')
  const description = ref('')
  const variant = ref<ToastVariant>('success')

  const variantClasses = computed(() => {
    if (variant.value === 'success') {
      return 'border-primary/30'
    }
    return 'border-destructive/40'
  })

  function showToast(
    payload: {
      title: string
      description?: string
      variant?: ToastVariant
      duration?: number
    }
  ) {
    title.value = payload.title
    description.value = payload.description ?? ''
    variant.value = payload.variant ?? 'success'
    visible.value = true

    setTimeout(() => {
      visible.value = false
    }, payload.duration ?? 4000)
  }

  defineExpose({ showToast })
  </script>

  <style>
  .fade-enter-active,
  .fade-leave-active {
    transition: opacity 0.25s ease, transform 0.25s ease;
  }
  .fade-enter-from,
  .fade-leave-to {
    opacity: 0;
    transform: translateY(-6px);
  }
  </style>
