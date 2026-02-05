<!--TODO: AINDA NÃO ESTÁ COMPLETAMENTE CORRETO-->
<template>
  <section class="min-h-screen flex items-center justify-center bg-gray-50 py-12">
    <div class="w-full max-w-md">
      <Card class="overflow-hidden shadow-lg">

        <div class="px-6 py-8 text-center">
          <p class="text-gray-500 mb-4">Reset your password</p>
          <p class="text-gray-400 text-sm">Enter your email to receive a reset code</p>
        </div>

        <div class="px-6 pb-8">
          <form @submit.prevent="sendCode">

            <div class="mb-4">
              <Label class="block text-sm mb-2">Email</Label>
              <Input
                type="email"
                v-model="email"
                placeholder="Enter your email"
              />
            </div>

            <Button class="w-full bg-purple-600 text-white py-2 rounded">
              Send Code
            </Button>

          </form>
        </div>
      </Card>

      <div class="flex justify-between mt-4 text-sm text-gray-500">
        <RouterLink to="/login">Back to login</RouterLink>
      </div>
    </div>

    <Toast ref="toastRef" />
  </section>
</template>

<script setup lang="ts">
import { ref } from "vue"
import { useRouter } from "vue-router"

import { Card } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Label } from "@/components/ui/label"
import { Input } from "@/components/ui/input"
import Toast from "@/components/ui/custom/Toast/Toast.vue"

const router = useRouter()
const toastRef = ref<InstanceType<typeof Toast> | null>(null)

const email = ref("")

function success(title: string, description?: string) {
  toastRef.value?.showToast({ title, description, variant: "success" })
}

function error(title: string, description?: string) {
  toastRef.value?.showToast({ title, description, variant: "error" })
}

async function sendCode() {
  if (!email.value) {
    error("Missing email", "Insert your email")
    return
  }

  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.value)) {
    error("Invalid email", "Insert a valid email")
    return
  }

  localStorage.setItem("email", JSON.stringify(email.value))

  success("Code sent", "Check your email")

  setTimeout(() => router.push("/forgetPassword/resetPassword"), 800)
}
</script>
