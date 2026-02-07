// src/composables/useToast.ts
import { ref } from 'vue';
import Toast from '@/components/ui/custom/Toast/Toast.vue'; 

const toastRef = ref<InstanceType<typeof Toast> | null>(null);

function success(title: string, description?: string) {
  toastRef.value?.showToast({ title, description, variant: 'success' });
}

function showError(title: string, description?: string) {
  toastRef.value?.showToast({ title, description, variant: 'error' });
}

export function useToast() {
  return { success, showError, toastRef };
}