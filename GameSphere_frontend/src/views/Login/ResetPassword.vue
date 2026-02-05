
<!--TODO: AINDA NÃO ESTÁ COMPLETAMENTE CORRETO-->
<template>
  <section class="min-h-screen flex items-center justify-center bg-gray-50 py-12">
    <div class="w-full max-w-md">
      <Card class="overflow-hidden shadow-lg">

        <!-- Header -->
        <div class="px-6 py-8 text-center">
          <p class="text-gray-500 mb-4">Reset your password</p>
        </div>

        <div class="px-6 pb-8">
          <!-- Vee-Validate Form -->
          <VForm @submit="onSubmit" v-slot="{ values, errors }">

            <!-- Code -->
            <VField name="code" v-slot="{ field, errorMessage }">
              <FormItem class="mb-4">
                <FormLabel>Code</FormLabel>
                <FormControl>
                  <Input placeholder="Enter 6-digit code" v-bind="field" />
                </FormControl>
                <FormMessage>{{ errorMessage }}</FormMessage>
              </FormItem>
            </VField>

            <!-- Password -->
            <VField name="password" v-slot="{ field, errorMessage }">
              <FormItem class="mb-2">
                <FormLabel>New Password</FormLabel>
                <FormControl>
                  <Input type="password" placeholder="Enter new password" v-bind="field" />
                </FormControl>

                <!-- Password Strength Bar -->
                <div v-if="values.password" class="w-full h-2 bg-gray-200 rounded mb-2">
                  <div
                    class="h-2 rounded transition-all duration-300"
                    :class="{
                      'bg-red-500 w-1/3': passwordStrength(values.password) === 'Weak',
                      'bg-yellow-500 w-2/3': passwordStrength(values.password) === 'Medium',
                      'bg-green-600 w-full': passwordStrength(values.password) === 'Strong'
                    }"
                  />
                </div>

                <!-- Password Strength Text -->
                <p v-if="values.password"
                  class="text-sm mb-4"
                  :class="{
                    'text-red-500': passwordStrength(values.password) === 'Weak',
                    'text-yellow-500': passwordStrength(values.password) === 'Medium',
                    'text-green-600': passwordStrength(values.password) === 'Strong'
                  }"
                >
                  Password {{ passwordStrength(values.password) }}
                </p>

                <FormMessage>{{ errorMessage }}</FormMessage>
              </FormItem>
            </VField>

            <!-- Confirm Password -->
            <VField name="password2" v-slot="{ field, errorMessage }">
              <FormItem class="mb-4">
                <FormLabel>Confirm Password</FormLabel>
                <FormControl>
                  <Input type="password" placeholder="Confirm new password" v-bind="field" />
                </FormControl>
                <FormMessage>{{ errorMessage }}</FormMessage>
              </FormItem>
            </VField>

            <Button type="submit" class="w-full bg-purple-600 text-white py-2 rounded">
              Reset Password
            </Button>

          </VForm>
        </div>
      </Card>

      <div class="flex justify-between mt-4 text-sm text-gray-500">
        <RouterLink to="/login">Back to login</RouterLink>
      </div>
    </div>

    <!-- Toast -->
    <Toast ref="toastRef" />
  </section>
</template>

<script setup lang="ts">
import { ref } from "vue"
import { useRouter } from "vue-router"
import { Form as VForm, Field as VField, useForm as useVForm } from "vee-validate"
import { toTypedSchema } from "@vee-validate/zod"
import * as z from "zod"

import { Card } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { FormItem, FormLabel, FormControl, FormMessage } from "@/components/ui/form"
import Toast from "@/components/ui/custom/Toast/Toast.vue"
import { RouterLink } from "vue-router"

const router = useRouter()
const toastRef = ref<InstanceType<typeof Toast> | null>(null)
const isSubmitting = ref(false)

/* Vee-Validate Schema */
useVForm({
  validationSchema: toTypedSchema(
    z.object({
      code: z.string().length(6, "Code must have 6 digits"),
      password: z.string().min(6, "Password must be at least 6 characters"),
      password2: z.string().min(6, "Confirm password is required"),
    })
  )
})

/* Password Strength */
function passwordStrength(password: string) {
  if (!password || password.length < 6) return 'Weak'
  if (password.length < 10) return /\d/.test(password) && /[a-zA-Z]/.test(password) ? 'Medium' : 'Weak'
  return /\d/.test(password) && /[a-zA-Z]/.test(password) && /[\W_]/.test(password) ? 'Strong' : 'Medium'
}

/* Toast Helpers */
function success(title: string, description?: string) {
  toastRef.value?.showToast({ title, description, variant: "success" })
}

function error(title: string, description?: string) {
  toastRef.value?.showToast({ title, description, variant: "error" })
}

/* Submit */
const onSubmit = async (values: any) => {
  isSubmitting.value = true
  try {
    // Verifica se passwords coincidem
    if (values.password !== values.password2) {
      error("Passwords do not match")
      return
    }

    if (passwordStrength(values.password) === "Weak") {
      error("Weak password", "Choose a stronger password")
      return
    }

    // Simula reset
    success("Password updated", "You can now login")
    setTimeout(() => router.push("/login"), 900)
  } catch {
    error("Failed to reset password")
  } finally {
    isSubmitting.value = false
  }
}
</script>

