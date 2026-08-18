<template>
  <section class="min-h-screen flex items-center justify-center bg-gray-50 py-12">
    <div class="w-full max-w-md">
      <Card class="overflow-hidden shadow-lg">

        <div class="px-6 py-8 text-center">
          <p class="text-gray-500 mb-4">Log in with</p>

          <div class="flex justify-center space-x-4 mb-6">
            <Button variant="outline" size="icon" @click="handleGoogleLogin">
              <img src="@/assets/logos/google.png" class="w-6 h-6" alt="google"/>
            </Button>

            <Button variant="outline" size="icon" @click="handleGithubLogin">
              <img src="@/assets/logos/github.png" class="w-6 h-6" alt="github"/>
            </Button>
          </div>

          <p class="text-gray-400">Or use your credentials</p>
        </div>

        <div class="px-6 pb-8">
          <form @submit.prevent="onSubmit">

            <FormField name="email" v-slot="{ componentField }">
              <FormItem class="mb-4">
                <FormLabel>Email</FormLabel>
                <FormControl>
                  <Input type="email" placeholder="Enter your email" v-bind="componentField"
                         @focus="clearError('email')"/>
                </FormControl>
                <FormMessage />
              </FormItem>
            </FormField>

            <FormField name="password" v-slot="{ componentField }">
              <FormItem class="mb-4">
                <FormLabel>Password</FormLabel>
                <FormControl>
                  <Input type="password" placeholder="Enter your password" v-bind="componentField"
                         @focus="clearError('password')"/>
                </FormControl>
                <FormMessage />
              </FormItem>
            </FormField>

            <FormField name="rememberMe" v-slot="{ value, handleChange }">
              <FormItem class="flex flex-col mb-4">
                <div class="flex items-center space-x-2 ml-auto">
                  <FormControl>
                    <Checkbox :model-value="value" @update:model-value="handleChange"/>
                  </FormControl>
                  <FormLabel class="!mt-0 cursor-pointer text-sm">Remember me</FormLabel>
                </div>
                <FormMessage />
              </FormItem>
            </FormField>

            <Button
              type="submit"
              class="w-full bg-purple-600 hover:bg-purple-700 text-white py-2 rounded"
              :disabled="isSubmitting"
            >
              <span v-if="isSubmitting">Logging in...</span>
              <span v-else>Log In</span>
            </Button>
          </form>
        </div>
      </Card>

      <div class="flex justify-between mt-4 text-sm text-gray-500">
        <RouterLink to="/register" class="hover:text-purple-600">Don't have an account?</RouterLink>
        <RouterLink to="/forgetPassword/sentCode" class="hover:text-purple-600">Forgot password?</RouterLink>
      </div>
    </div>

    <Toast ref="toastRef" />
  </section>
</template>

<script setup lang="ts">
/**
 * LoginView Component
 * * Handles user authentication via email/password and social providers (Google/GitHub).
 * Integrates with Firebase for social auth and a custom backend service for session management.
 * Includes form validation using Zod and a custom `useAppForm` composable.
 */

import { ref } from "vue"
import { useRouter, RouterLink } from "vue-router"
import * as z from "zod"

import { FormField, FormItem, FormLabel, FormControl, FormMessage } from "@/components/ui/form"
import { Input } from "@/components/ui/input"
import { Checkbox } from "@/components/ui/checkbox"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import Toast from "@/components/ui/custom/Toast/Toast.vue"

import { login, social_login } from "@/services/authService"
import { auth, googleProvider, githubProvider, signInWithPopup } from "@/services/firebase"
import { useAppForm} from "@/composables/useAppForm";
import { useToast } from '@/composables/useToast';

const router = useRouter()
const isSubmitting = ref(false)

const { success, showError, toastRef } = useToast();

/* 🧾 FORM CONFIG */
const loginSchema =
    z.object({
      email: z.string().min(1, "Email is required").email("Invalid email"),
      password: z.string().min(1, "Password is required").min(8, "Invalid Password"),
    });

const { form, clearError } = useAppForm(loginSchema)

/* 🚀 SUBMIT */
const onSubmit = form.handleSubmit(async (values) => {
  isSubmitting.value = true
  try {
    const userData = await login(values.email, values.password)

    if (!userData) {
      showError("Login failed", "Unable to retrieve user data")
      return
    }

    localStorage.setItem("user", JSON.stringify(userData.user))
    window.dispatchEvent(new Event("user-logged-in"));
    success("Welcome!", "Logged in successfully")

    await router.push("/profile")
  } catch (err: any) {
    // API errors continue to show in the Toast
    showError("Login Error", err?.response?.data?.message || "Invalid credentials")
  } finally {
    isSubmitting.value = false
  }
})

/* 🌐 SOCIAL LOGIN */
async function handleGoogleLogin() {
  try {
    const { user } = await signInWithPopup(auth, googleProvider)
    const response = await social_login(user.uid, user.email!)
    localStorage.setItem("user", JSON.stringify(response.user))
    success("Google Login", "Logged in successfully")
    await router.push("/profile")
  } catch {
    showError("Google login failed")
  }
}

async function handleGithubLogin() {
  try {
    const { user } = await signInWithPopup(auth, githubProvider)
    const response = await social_login(user.uid, user.email!)
    localStorage.setItem("user", JSON.stringify(response.user))
    success("GitHub Login", "Logged in successfully")
    await router.push("/profile")
  } catch {
    showError("GitHub login failed")
  }
}
</script>