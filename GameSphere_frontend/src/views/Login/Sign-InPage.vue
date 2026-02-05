<template>
  <section class="min-h-screen flex items-center justify-center bg-gray-50 py-12">
    <div class="w-full max-w-md">
      <Card class="overflow-hidden shadow-lg">

        <!-- Social -->
        <div class="px-6 py-8 text-center">
          <p class="text-gray-500 mb-4">Sign in with</p>

          <div class="flex justify-center space-x-4 mb-6">
            <Button variant="outline" size="icon" @click="handleGoogleLogin">
              <img src="@/assets/logos/google.png" class="w-6 h-6" alt="google"/>
            </Button>

            <Button variant="outline" size="icon" @click="handleGithubLogin">
              <img src="@/assets/logos/github.png" class="w-6 h-6" alt="github"/>
            </Button>
          </div>

          <p class="text-gray-400">Or sign in with credentials</p>
        </div>

        <!-- FORM CHADCN -->
        <div class="px-6 pb-8">
          <Form :form="form" @submit="onSubmit" v-slot=" { values }">

            <!-- EMAIL -->
            <FormField name="email" v-slot=" { componentField }">
              <FormItem class="mb-4">
                <FormLabel>Email</FormLabel>
                <FormControl>
                  <Input type="email" placeholder="Enter your email" v-bind="componentField"/>
                </FormControl>
                <FormMessage/>
              </FormItem>
            </FormField>

            <!-- PASSWORD -->
            <FormField name="password" v-slot=" { componentField }">
              <FormItem class="mb-4">
                <FormLabel>Password</FormLabel>
                <FormControl>
                  <Input type="password" placeholder="Enter your password" v-bind="componentField"/>
                </FormControl>
                <FormMessage/>
              </FormItem>
            </FormField>

            <!-- REMEMBER -->
            <FormField name="rememberMe" type="checkbox" v-slot=" { value, handleChange }" >
              <FormItem class="flex mb-4">
                <div class="flex items-center space-x-2 ml-auto">
                  <FormControl>
                    <Checkbox :model-value="value" @update:modelValue="handleChange"/>
                  </FormControl>
                  <FormLabel class="!mt-0">Remember me</FormLabel>
                </div>
              </FormItem>
            </FormField>

            <Button type="submit" class="w-full bg-purple-600 text-white py-2 rounded">
              Sign In
            </Button>
          </Form>
        </div>
      </Card>
      <div class="flex justify-between mt-4 text-sm text-gray-500">
        <RouterLink to="/register">Dont have an account?</RouterLink>
        <RouterLink to="/forgetPassword/sentCode">Forgot password?</RouterLink>
      </div>
    </div>
    <Toast ref="toastRef" />
  </section>
</template>

<script setup lang="ts">
import { ref } from "vue"
import { useRouter } from "vue-router"
import { useForm } from "vee-validate"
import { toTypedSchema } from "@vee-validate/zod"
import * as z from "zod"

import { Form, FormField, FormItem, FormLabel, FormControl, FormMessage } from "@/components/ui/form"
import { Input } from "@/components/ui/input"
import { Checkbox } from "@/components/ui/checkbox"
import { Button } from "@/components/ui/button"

import { login, social_login } from "@/services/authService"
import { auth, googleProvider, githubProvider, signInWithPopup } from "@/services/firebase"
import {Card} from "@/components/ui/card";
import Toast from "@/components/ui/custom/Toast/Toast.vue";

const router = useRouter()
const toastRef = ref<InstanceType<typeof Toast> | null>(null)

function success(title: string, description?: string) {
  toastRef.value?.showToast({ title, description, variant: 'success' })
}

function error(title: string, description?: string) {
  toastRef.value?.showToast({ title, description, variant: 'error' })
}

/* FORM */
const form = useForm({
  validationSchema: toTypedSchema(
    z.object({
      email: z.string().email(),
      password: z.string().min(1),
      rememberMe: z.boolean().default(false)
    })
  )
})

const { handleSubmit } = form

/* SUBMIT */
const onSubmit = handleSubmit(async values => {
  try {
    const response = await login(values.email, values.password)

    if (!response.data)
      return error("Login failed", "No data received")

    localStorage.setItem("user", JSON.stringify(response.data))

    success("Welcome back!", "Login efetuado com sucesso")
    await router.push("/profile")

  } catch (err:any) {
    error("Login failed", err?.response?.data?.message || "Credenciais inválidas")
  }
})

/* SOCIAL */
async function handleGoogleLogin() {
  try {
    const { user } = await signInWithPopup(auth, googleProvider)
    const response = await social_login(user.uid, user.email)

    localStorage.setItem("user", JSON.stringify(response.user))

    success("Google login", "Sessão iniciada com sucesso")
    await router.push("/profile")

  } catch {
    error("Google login failed")
  }
}

async function handleGithubLogin() {
  try {
    const { user } = await signInWithPopup(auth, githubProvider)
    const response = await social_login(user.uid, user.email)

    localStorage.setItem("user", JSON.stringify(response.user))

    success("GitHub login", "Sessão iniciada com sucesso")
    await router.push("/profile")

  } catch {
    error("GitHub login failed")
  }
}
</script>
