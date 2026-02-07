<template>
  <section class="min-h-screen flex items-center justify-center bg-gray-50 py-12">
    <div class="w-full max-w-md">
      <Card class="overflow-hidden shadow-lg">
        <div class="px-6 py-8 text-center">
          <p class="text-gray-500 mb-2 font-medium">Set New Password</p>
          <p class="text-gray-400 text-sm">
            Enter the code received by email and your new password
          </p>
        </div>

        <div class="px-6 pb-8">
          <form @submit.prevent="onSubmit">

            <FormField name="resetCode" v-slot="{ componentField }">
              <FormItem class="mb-4">
                <FormLabel>Code</FormLabel>
                <FormControl>
                  <Input placeholder="Received code" v-bind="componentField"
                         @focus="clearError('resetCode')"/>
                </FormControl>
                <FormMessage />
              </FormItem>
            </FormField>

            <FormField name="password" v-slot="{ componentField }">
              <FormItem class="mb-4">
                <FormLabel>New Password</FormLabel>
                <FormControl>
                  <Input type="password" placeholder="Minimum 8 characters" v-bind="componentField"
                         @focus="clearError('password')"/>
                </FormControl>

                <div v-if="form.values.password" class="w-full h-1.5 bg-gray-200 rounded mt-3 mb-1">
                  <div
                    class="h-1.5 rounded transition-all duration-300"
                    :class="{
                      'bg-red-500 w-1/3': passwordStrength(form.values.password) === 'Weak',
                      'bg-yellow-500 w-2/3': passwordStrength(form.values.password) === 'Medium',
                      'bg-green-600 w-full': passwordStrength(form.values.password) === 'Strong'
                    }"
                  />
                </div>
                <p v-if="form.values.password" class="text-[10px] uppercase font-bold tracking-wider" :class="{
                  'text-red-500': getPasswordStrength(form.values.password) === 'Weak',
                  'text-yellow-500': getPasswordStrength(form.values.password) === 'Medium',
                  'text-green-600': getPasswordStrength(form.values.password) === 'Strong'
                }">
                  Security: {{ passwordStrength(form.values.password) }}
                </p>
                <FormMessage />
              </FormItem>
            </FormField>

            <FormField name="confirmPassword" v-slot="{ componentField }">
              <FormItem class="mb-6">
                <FormLabel>Confirm Password</FormLabel>
                <FormControl>
                  <Input type="password" placeholder="Repeat the password" v-bind="componentField"
                         @focus="clearError('confirmPassword')"/>
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
              <span v-else>Change Password</span>
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

import { resetPassword } from "@/services/authService"
import { getPasswordStrength } from "@/utils/passwordStrength";
import { useToast } from '@/composables/useToast';
import {useAppForm} from "@/composables/useAppForm";

const router = useRouter()
const isSubmitting = ref(false)

const { success, showError, toastRef } = useToast();

const resetPasswordSchema = z.object({
      resetCode: z.string().min(6, "The code must be 6 characters"),
      password: z.string().min(8, "The password must be at least 8 characters"),
      confirmPassword: z.string().min(1, "Confirm your password")
    }).refine((data) => data.password === data.confirmPassword, {
      message: "Passwords do not match",
      path: ["confirmPassword"],
    });

const {form, clearError} = useAppForm(resetPasswordSchema);

const onSubmit = form.handleSubmit(async (values) => {
  isSubmitting.value = true
  try {
    const emailData = localStorage.getItem("email")
    const email = emailData ? JSON.parse(emailData) : null

    if (!email) {
      error("Session Error", "Email not found. Please try to recover again.")
      return
    }

    await resetPassword(email, values.resetCode, values.password)
    localStorage.removeItem("email")

    success("Success", "Password changed successfully!")
    setTimeout(() => router.push("/login"), 1500)
  } catch (err: any) {
    error("Error", err?.response?.data?.message || "Invalid or expired code")
  } finally {
    isSubmitting.value = false
  }
})
</script>