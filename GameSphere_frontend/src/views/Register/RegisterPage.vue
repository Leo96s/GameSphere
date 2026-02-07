<template>
  <section class="min-h-screen flex items-center justify-center bg-gray-50 py-12">
    <div class="w-full max-w-md">
      <Card class="overflow-hidden shadow-lg">

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
          <p class="text-gray-400">Or create an account manually</p>
        </div>

        <div class="px-6 pb-8">
          <form @submit.prevent="onSubmit">

            <div class="grid grid-cols-2 gap-4">
              <FormField name="firstName" v-slot="{ componentField }">
                <FormItem class="mb-4">
                  <FormLabel>First Name</FormLabel>
                  <FormControl>
                    <Input placeholder="E.g., John" v-bind="componentField"
                           @focus="clearError('firstName')"/>
                  </FormControl>
                  <FormMessage /> </FormItem>
              </FormField>

              <FormField name="lastName" v-slot="{ componentField }">
                <FormItem class="mb-4">
                  <FormLabel>Last Name</FormLabel>
                  <FormControl>
                    <Input placeholder="E.g., Doe" v-bind="componentField"
                           @focus="clearError('lastName')"/>
                  </FormControl>
                  <FormMessage />
                </FormItem>
              </FormField>
            </div>

            <FormField name="email" v-slot="{ componentField }">
              <FormItem class="mb-4">
                <FormLabel>Email</FormLabel>
                <FormControl>
                  <Input type="email" placeholder="your@email.com" v-bind="componentField"
                         @focus="clearError('email')"/>
                </FormControl>
                <FormMessage />
              </FormItem>
            </FormField>

            <FormField name="password" v-slot="{ componentField, value }">
              <FormItem class="mb-2">
                <FormLabel>Password</FormLabel>
                <FormControl>
                  <Input type="password" placeholder="Minimum 8 characters" v-bind="componentField"
                         @focus="clearError('password')"/>
                </FormControl>
                <FormMessage />
              </FormItem>

              <div v-if="value" class="w-full h-1.5 bg-gray-200 rounded mb-4">
                <div
                  class="h-1.5 rounded transition-all duration-300"
                  :class="{
                    'bg-red-500 w-1/3': getPasswordStrength(value) === 'Weak',
                    'bg-yellow-500 w-2/3': getPasswordStrength(value) === 'Medium',
                    'bg-green-600 w-full': getPasswordStrength(value) === 'Strong'
                  }"
                />
              </div>
            </FormField>

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

            <FormField name="acceptedPolicy" v-slot="{ value, handleChange }">
              <FormItem class="mb-6">
                <div class="flex items-center space-x-2">
                  <FormControl>
                    <Checkbox :model-value="value"
                              @update:model-value="handleChange" />
                  </FormControl>
                  <FormLabel class="!mt-0 cursor-pointer text-xs">
                    I agree with the Privacy Policy
                  </FormLabel>
                </div>
                <FormMessage />
              </FormItem>
            </FormField>

            <Button
              type="submit"
              class="w-full bg-purple-600 hover:bg-purple-700 text-white py-2 rounded"
              :disabled="isSubmitting"
            >
              <span v-if="isSubmitting">Processing...</span>
              <span v-else>Create Account</span>
            </Button>
          </form>
        </div>
      </Card>

      <div class="flex justify-center mt-4 text-sm text-gray-500">
        <RouterLink to="/login" class="hover:text-purple-600 transition-colors">
          Already have an account? Log in
        </RouterLink>
      </div>
    </div>

    <Toast ref="toastRef" />
  </section>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import * as z from 'zod'

import Toast from '@/components/ui/custom/Toast/Toast.vue'
import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Checkbox } from '@/components/ui/checkbox'
import { Select, SelectTrigger, SelectContent, SelectItem, SelectValue } from '@/components/ui/select'
import { FormField, FormItem, FormLabel, FormControl, FormMessage } from '@/components/ui/form'

import User from '@/models/User'
import { Gender } from '@/enums/Gender'
import { createUser } from '@/services/userServices'
import { auth, googleProvider, githubProvider, signInWithPopup } from '@/services/firebase'
import { useToast } from '@/composables/useToast';
import { getPasswordStrength } from '@/utils/passwordStrength'
import { useAppForm } from "@/composables/useAppForm";

const router = useRouter()
const isSubmitting = ref(false)

const { success, showError, toastRef } = useToast();

const registerSchema = z.object({
      firstName: z.string().min(1, "First name is required"),
      lastName: z.string().min(1, "Last name is required"),
      email: z.string().min(1, "Email is required").email("Invalid email format"),
      password: z.string().min(8, "Minimum 8 characters"),
      gender: z.string().min(1, "Select a gender"),
      acceptedPolicy: z.boolean().refine(v => v, "You must accept the terms"),
    });

const { form, clearError } = useAppForm(registerSchema)

const onSubmit = form.handleSubmit(async (values) => {
  isSubmitting.value = true
  try {
    await createUser(new User(values))
    success('Account created!', 'Redirecting...')
    setTimeout(() => router.push('/login'), 1500)
  } catch (err: any) {
    showError('Error', err?.response?.data?.message || 'Failed to register')
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
    success('Account created via Google!')
    await router.push('/login')
  } catch {
    showError('Error registering with Google')
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
    success('Account created via GitHub!')
    await router.push('/login')
  } catch {
    showError('Error registering with GitHub')
  }
}
</script>