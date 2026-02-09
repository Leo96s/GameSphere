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

/**
 * Defines the supported visual styles for the Toast.
 */
type ToastVariant = 'success' | 'error'

/** Controls the visibility of the toast. */
const visible = ref(false)
/** The main heading of the notification. */
const title = ref('')
/** Optional detailed message. */
const description = ref('')
/** The current active visual variant. */
const variant = ref<ToastVariant>('success')

/**
 * Computes dynamic Tailwind classes based on the active variant.
 * @returns {string} Border classes for the toast container.
 */
const variantClasses = computed(() => {
  if (variant.value === 'success') {
    return 'border-primary/30'
  }
  return 'border-destructive/40'
})

/**
 * Displays the Toast notification with the specified parameters.
 * @param {Object} payload - Configuration object for the toast.
 * @param {string} payload.title - The headline message.
 * @param {string} [payload.description] - Supporting text message.
 * @param {ToastVariant} [payload.variant='success'] - Visual style (success | error).
 * @param {number} [payload.duration=4000] - Time in ms before auto-closing.
 * * @example
 * toastRef.value.showToast({
 * title: 'Changes Saved',
 * variant: 'success'
 * });
 **/
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

/**
 * Exposes the showToast method to parent components via template refs.
 */
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
