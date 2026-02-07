<template>
  <section class="min-h-screen flex items-center justify-center bg-gray-50 py-12">
    <div class="w-full max-w-md">
      <Card class="overflow-hidden shadow-lg">

        <div class="px-6 py-8 text-center">
          <p class="text-gray-500 mb-2 font-medium">Recover Password</p>
          <p class="text-gray-400 text-sm">
            Enter your email to receive a verification code
          </p>
        </div>

        <div class="px-6 pb-8">
          <form @submit.prevent="onSubmit">

            <FormField name="email" v-slot="{ componentField }">
              <FormItem class="mb-4">
                <FormLabel>Email</FormLabel>
                <FormControl>
                  <Input
                    type="email"
                    placeholder="example@email.com"
                    v-bind="componentField"
                    @focus="clearError('email')"
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            </FormField>

            <Button
              type="submit"
              class="w-full bg-purple-600 hover:bg-purple-700 text-white py-2 rounded"
              :disabled="isSubmitting"
            >
              <span v-if="isSubmitting">Sending...</span>
              <span v-else>Send Code</span>
            </Button>

          </form>
        </div>
      </Card>

      <div class="flex justify-between mt-4 text-sm text-gray-500">
        <RouterLink to="/login" class="hover:text-purple-600 transition-colors">
          Back to login
        </RouterLink>
      </div>
    </div>

    <Toast ref="toastRef" />
  </section>
</template>

<script setup lang="ts">
import { ref } from "vue"
import { useRouter, RouterLink } from "vue-router"
import * as z from "zod"

import { Card } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { FormField, FormItem, FormLabel, FormControl, FormMessage } from "@/components/ui/form"
import Toast from "@/components/ui/custom/Toast/Toast.vue"

import { sentResetCode } from "@/services/authService"
import { useToast } from '@/composables/useToast';
import {useAppForm} from "@/composables/useAppForm";

const router = useRouter()
const isSubmitting = ref(false)

const { success, showError, toastRef } = useToast();

const sentCodeSchema = z.object({
      email: z.string()
        .min(1, "Email is required")
        .email("Enter a valid email"),
    });

const {form, clearError} = useAppForm(sentCodeSchema);

const onSubmit = form.handleSubmit(async (values) => {
  isSubmitting.value = true

  try {
    await sentResetCode(values.email)

    // Store the email for use in the next step (reset password)
    localStorage.setItem("email", JSON.stringify(values.email))

    success("Code sent", "Check your inbox")

    setTimeout(() => {
      router.push("/forgetPassword/resetPassword")
    }, 1000)

  } catch (err: any) {
    showError(
      "Error sending",
      err?.response?.data?.message || "Could not send the code"
    )
  } finally {
    isSubmitting.value = false
  }
})
</script>