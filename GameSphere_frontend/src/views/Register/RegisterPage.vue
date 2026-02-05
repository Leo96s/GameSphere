<template>
  <section class="min-h-screen flex items-center justify-center bg-gray-50 py-12">
    <div class="w-full max-w-md">
      <Card class="overflow-hidden shadow-lg">

        <!-- Header Social -->
        <div class="px-6 py-8 text-center">
          <p class="text-gray-500 mb-4">Register with</p>
          <div class="flex justify-center space-x-4 mb-6">
            <Button variant="outline" size="icon" @click="registerWithGoogle">
              <img src="@/assets/logos/google.png" class="w-6 h-6" alt="google" />
            </Button>
            <Button variant="outline" size="icon" @click="registerWithGithub">
              <img src="@/assets/logos/github.png" class="w-6 h-6" alt="github" />
            </Button>
          </div>
          <p class="text-gray-400">Or register with credentials</p>
        </div>

        <!-- Form -->
        <div class="px-6 pb-8">
          <Form :form="form" @submit="onSubmit" v-slot="{ values }">
            <FormField name="firstName" v-slot="{ componentField }">
              <FormItem class="mb-4">
                <FormLabel>First Name</FormLabel>
                <FormControl>
                  <Input placeholder="Enter your first name" v-bind="componentField" />
                </FormControl>
                <FormMessage />
              </FormItem>
            </FormField>

            <FormField name="lastName" v-slot="{ componentField }">
              <FormItem class="mb-4">
                <FormLabel>Last Name</FormLabel>
                <FormControl>
                  <Input placeholder="Enter your last name" v-bind="componentField" />
                </FormControl>
                <FormMessage />
              </FormItem>
            </FormField>

            <FormField name="email" v-slot="{ componentField }">
              <FormItem class="mb-4">
                <FormLabel>Email</FormLabel>
                <FormControl>
                  <Input type="email" placeholder="Enter your email" v-bind="componentField" />
                </FormControl>
                <FormMessage />
              </FormItem>
            </FormField>

            <!-- PASSWORD -->
            <FormField name="password" v-slot="{ componentField }">
              <FormItem class="mb-2">
                <FormLabel>Password</FormLabel>
                <FormControl>
                  <Input
                    type="password"
                    placeholder="Enter your password"
                    v-bind="componentField"
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            </FormField>

            <!-- PASSWORD STRENGTH BAR -->
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

            <!-- PASSWORD STRENGTH TEXT -->
            <p
              v-if="values.password"
              class="text-sm mb-4"
              :class="{
                'text-red-500': passwordStrength(values.password) === 'Weak',
                'text-yellow-500': passwordStrength(values.password) === 'Medium',
                'text-green-600': passwordStrength(values.password) === 'Strong'
              }"
            >
            </p>

            <!-- GENDER -->
            <FormField name="gender" v-slot="{ componentField }">
              <FormItem class="mb-4">
                <FormLabel>Gender</FormLabel>
                <Select v-bind="componentField">
                  <FormControl>
                    <SelectTrigger>
                      <SelectValue placeholder="Select your gender" />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    <SelectItem value="male">Male</SelectItem>
                    <SelectItem value="female">Female</SelectItem>
                    <SelectItem value="other">Other</SelectItem>
                  </SelectContent>
                </Select>
                <FormMessage />
              </FormItem>
            </FormField>

            <!-- POLICY -->
            <FormField name="acceptedPolicy" type="checkbox" v-slot="{ value, handleChange }">
              <FormItem class="flex items-center space-x-2 mb-4">
                <FormControl>
                  <Checkbox :model-value="value" @update:modelValue="handleChange" />
                </FormControl>
                <FormLabel class="!mt-0">
                  I agree with the
                  <RouterLink to="#" class="text-purple-600 underline">
                    Privacy Policy
                  </RouterLink>
                </FormLabel>
                <FormMessage />
              </FormItem>
            </FormField>

            <Button
              type="submit"
              class="w-full bg-purple-600 text-white py-2 rounded"
              :disabled="isSubmitting"
            >
              Create Account
            </Button>
          </Form>
        </div>
      </Card>

      <div class="flex justify-center mt-4 text-sm text-gray-500">
        <RouterLink to="/login">Already have an account?</RouterLink>
      </div>
    </div>

    <!-- TOAST -->
    <Toast ref="toastRef" />
  </section>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import * as z from 'zod'

import Toast from '@/components/ui/custom/Toast/Toast.vue'
import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Checkbox } from '@/components/ui/checkbox'
import {
  Select,
  SelectTrigger,
  SelectContent,
  SelectItem,
  SelectValue
} from '@/components/ui/select'
import {
  Form,
  FormField,
  FormItem,
  FormLabel,
  FormControl,
  FormMessage
} from '@/components/ui/form'

import User from '@/models/User'
import { Gender } from '@/enums/Gender'
import { createUser } from '@/services/userServices'
import { auth, googleProvider, githubProvider, signInWithPopup } from '@/services/firebase'

const router = useRouter()
const toastRef = ref<InstanceType<typeof Toast> | null>(null)
const isSubmitting = ref(false)

/* ✅ FORM */
const form = useForm({
  validationSchema: toTypedSchema(
    z.object({
      firstName: z.string().min(1),
      lastName: z.string().min(1),
      email: z.string().email(),
      password: z.string().min(6),
      gender: z.string(),
      acceptedPolicy: z.boolean().refine(v => v),
    })
  )
})

const { handleSubmit } = form

function passwordStrength(password: string) {
  if (!password || password.length < 6) return 'Weak'

  if (password.length < 10) {
    return /\d/.test(password) && /[a-zA-Z]/.test(password)
      ? 'Medium'
      : 'Weak'
  }

  return /\d/.test(password) &&
    /[a-zA-Z]/.test(password) &&
    /[\W_]/.test(password)
    ? 'Strong'
    : 'Medium'
}

/* ✅ TOAST */
function success(title: string, description?: string) {
  toastRef.value?.showToast({ title, description, variant: 'success' })
}

function error(title: string, description?: string) {
  toastRef.value?.showToast({ title, description, variant: 'error' })
}

/* ✅ SUBMIT */
const onSubmit = handleSubmit(async values => {
  isSubmitting.value = true
  try {
    await createUser(new User(values))
    success('Account created', 'You can now login')
    router.push('/login')
  } catch {
    error('Registration failed', 'Please try again')
  } finally {
    isSubmitting.value = false
  }
})

/* ✅ SOCIAL LOGIN */
async function registerWithGoogle() {
  try {
    const { user } = await signInWithPopup(auth, googleProvider)
    await createUser(new User({
      firstName: user.displayName?.split(' ')[0],
      lastName: user.displayName?.split(' ')[1] ?? '',
      email: user.email,
      gender: Gender.Other,
      password: Math.random().toString(36).slice(-8),
      uid: user.uid,
    }))
    success('Account created with Google')
    router.push('/login')
  } catch {
    error('Google registration failed')
  }
}

async function registerWithGithub() {
  try {
    const { user } = await signInWithPopup(auth, githubProvider)
    await createUser(new User({
      firstName: 'GitHub',
      lastName: 'User',
      email: user.email,
      gender: Gender.Other,
      password: Math.random().toString(36).slice(-8),
      uid: user.uid,
    }))
    success('Account created with GitHub')
    router.push('/login')
  } catch {
    error('GitHub registration failed')
  }
}
</script>
