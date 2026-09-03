<template>
  <section class="min-h-screen flex items-center justify-center bg-gray-50 py-12">
    <div class="w-full max-w-md">
      <Card class="overflow-hidden shadow-lg">
        <div class="px-6 py-8 text-center">
          <p class="text-gray-500 mb-2 font-medium">Recover Your Account</p>
          <p class="text-gray-400 text-sm">
            Enter the code received by email to reactivate your account
          </p>
        </div>

        <div class="px-6 pb-8">
          <form @submit.prevent="onSubmit">

            <FormField name="code" v-slot="{ componentField }">
              <FormItem class="mb-4">
                <FormLabel>Code</FormLabel>
                <FormControl>
                  <Input placeholder="Received code" v-bind="componentField"
                         @focus="clearError('code')"/>
                </FormControl>
                <FormMessage />
              </FormItem>
            </FormField>

            <Button
              type="submit"
              class="w-full bg-purple-600 hover:bg-purple-700 text-white py-2 rounded"
              :disabled="isSubmitting"
            >
              <span v-if="isSubmitting">Processing...</span>
              <span v-else>Recover Account</span>
            </Button>
          </form>
        </div>
      </Card>
    </div>

    <Toast ref="toastRef" />
  </section>
</template>

<script setup lang="ts">
import { ref } from "vue"
import { useRouter } from "vue-router"
import * as z from "zod"

import { Card } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { FormField, FormItem, FormLabel, FormControl, FormMessage } from "@/components/ui/form"
import Toast from "@/components/ui/custom/Toast/Toast.vue"

import { recoverAccount } from "@/services/authService"
import { useToast } from '@/composables/useToast';
import {useAppForm} from "@/composables/useAppForm";

const router = useRouter()
const isSubmitting = ref(false)
const { success, showError, toastRef } = useToast();

const recoverAccountSchema = z.object({
      code: z.string().min(6, "The code must be 6 characters"),
    });

const {form, clearError} = useAppForm(recoverAccountSchema);

const onSubmit = form.handleSubmit(async (values) => {
  isSubmitting.value = true
  try {
    const emailData = localStorage.getItem("accountRecoveryEmail")
    const email = emailData ? JSON.parse(emailData) : null

    if (!email) {
      showError("Session Error", "Email not found. Please try to recover again.")
      return
    }

    await recoverAccount(email, values.code)
    localStorage.removeItem("accountRecoveryEmail")

    success("Success", "Account recovered successfully!")
    setTimeout(() => router.push("/login"), 1500)
  } catch (err: any) {
    showError("Error", err?.response?.data?.message || "Invalid or expired code")
  } finally {
    isSubmitting.value = false
  }
})
</script>
