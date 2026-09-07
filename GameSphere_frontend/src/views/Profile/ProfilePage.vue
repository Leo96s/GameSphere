<template>
  <section class="min-h-screen bg-gray-50 px-4 py-12">
    <div class="mx-auto max-w-2xl">
      <Card class="p-6 shadow-lg">
        <div class="mb-8">
          <p class="text-sm font-semibold uppercase tracking-wide text-purple-600">Account</p>
          <h1 class="mt-1 text-3xl font-bold text-gray-900">My profile</h1>
          <p class="mt-2 text-gray-500">Keep your personal information up to date.</p>
        </div>

        <div v-if="isLoading" class="py-8 text-center text-gray-500">Loading profile...</div>

        <form v-else class="space-y-5" @submit.prevent="saveProfile">
          <div class="grid gap-5 sm:grid-cols-2">
            <div>
              <label for="first-name" class="mb-2 block text-sm font-medium text-gray-700">First name</label>
              <Input id="first-name" v-model="form.firstName" autocomplete="given-name" />
            </div>

            <div>
              <label for="last-name" class="mb-2 block text-sm font-medium text-gray-700">Last name</label>
              <Input id="last-name" v-model="form.lastName" autocomplete="family-name" />
            </div>
          </div>

          <div>
            <label for="gender" class="mb-2 block text-sm font-medium text-gray-700">Gender</label>
            <select
              id="gender"
              v-model.number="form.gender"
              class="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px]"
            >
              <option v-for="option in genderOptions" :key="option.value" :value="option.value">
                {{ option.label }}
              </option>
            </select>
          </div>

          <p v-if="formError" class="text-sm text-red-600" role="alert">{{ formError }}</p>

          <div class="flex flex-col gap-3 border-t pt-6 sm:flex-row sm:justify-between">
            <Button type="submit" :disabled="isSaving" class="bg-purple-600 text-white hover:bg-purple-700">
              {{ isSaving ? 'Saving...' : 'Save changes' }}
            </Button>
            <Button type="button" variant="destructive" :disabled="isDeactivating" @click="deactivateAccount">
              {{ isDeactivating ? 'Deactivating...' : 'Deactivate account' }}
            </Button>
          </div>
        </form>

        <p v-if="user" class="mt-6 border-t pt-4 text-sm text-gray-500">
          Member {{ timeSinceRegistration }}.
        </p>
      </Card>

      <Card v-if="user" class="mt-6 p-6 shadow-lg">
        <h2 class="text-lg font-semibold text-gray-900">Change password</h2>
        <p class="mt-1 text-sm text-gray-500">Confirm your current password to set a new one.</p>

        <form class="mt-5 space-y-4" @submit.prevent="submitPasswordChange">
          <div>
            <label for="current-password" class="mb-2 block text-sm font-medium text-gray-700">Current password</label>
            <PasswordInput id="current-password" v-model="passwordForm.currentPassword" autocomplete="current-password" />
          </div>

          <div>
            <label for="new-password" class="mb-2 block text-sm font-medium text-gray-700">New password</label>
            <PasswordInput id="new-password" v-model="passwordForm.newPassword" autocomplete="new-password" />
          </div>

          <p v-if="passwordError" class="text-sm text-red-600" role="alert">{{ passwordError }}</p>

          <Button type="submit" :disabled="isChangingPassword" class="bg-purple-600 text-white hover:bg-purple-700">
            {{ isChangingPassword ? 'Updating...' : 'Update password' }}
          </Button>
        </form>
      </Card>

      <Card v-if="user" class="mt-6 p-6 shadow-lg">
        <h2 class="text-lg font-semibold text-gray-900">Change email</h2>
        <p class="mt-1 text-sm text-gray-500">
          We will send a confirmation code to the new address before applying the change.
        </p>

        <form v-if="!emailChangePending" class="mt-5 space-y-4" @submit.prevent="submitEmailChangeRequest">
          <div>
            <label for="new-email" class="mb-2 block text-sm font-medium text-gray-700">New email</label>
            <Input id="new-email" v-model="emailForm.newEmail" type="email" autocomplete="email" />
          </div>

          <div>
            <label for="email-current-password" class="mb-2 block text-sm font-medium text-gray-700">Current password</label>
            <PasswordInput id="email-current-password" v-model="emailForm.currentPassword" autocomplete="current-password" />
          </div>

          <p v-if="emailError" class="text-sm text-red-600" role="alert">{{ emailError }}</p>

          <Button type="submit" :disabled="isRequestingEmailChange" class="bg-purple-600 text-white hover:bg-purple-700">
            {{ isRequestingEmailChange ? 'Sending code...' : 'Send confirmation code' }}
          </Button>
        </form>

        <form v-else class="mt-5 space-y-4" @submit.prevent="submitEmailChangeConfirmation">
          <div>
            <label for="email-code" class="mb-2 block text-sm font-medium text-gray-700">Confirmation code</label>
            <Input id="email-code" v-model="emailForm.code" autocomplete="one-time-code" />
          </div>

          <p v-if="emailError" class="text-sm text-red-600" role="alert">{{ emailError }}</p>

          <Button type="submit" :disabled="isConfirmingEmailChange" class="bg-purple-600 text-white hover:bg-purple-700">
            {{ isConfirmingEmailChange ? 'Confirming...' : 'Confirm new email' }}
          </Button>
        </form>
      </Card>

      <Card v-if="user" class="mt-6 p-6 shadow-lg">
        <h2 class="text-lg font-semibold text-gray-900">Linked accounts</h2>
        <p class="mt-1 text-sm text-gray-500">
          Connect additional sign-in methods so you can log in with either one.
        </p>

        <p v-if="linkedAccountsError" class="mt-4 text-sm text-red-600" role="alert">{{ linkedAccountsError }}</p>

        <div v-if="!identityVerified" class="mt-5 space-y-3">
          <p class="text-sm text-gray-500">Confirm your identity to manage linked accounts.</p>
          <div class="flex flex-wrap gap-3">
            <Button
              type="button"
              variant="outline"
              :disabled="isVerifying['google.com']"
              @click="verifyIdentity('google.com')"
            >
              {{ isVerifying['google.com'] ? 'Verifying...' : 'Verify with Google' }}
            </Button>
            <Button
              type="button"
              variant="outline"
              :disabled="isVerifying['github.com']"
              @click="verifyIdentity('github.com')"
            >
              {{ isVerifying['github.com'] ? 'Verifying...' : 'Verify with GitHub' }}
            </Button>
          </div>
        </div>

        <div v-else class="mt-5 space-y-3">
          <div
            v-for="provider in providerOptions"
            :key="provider.id"
            class="flex items-center justify-between border-b pb-3 last:border-b-0 last:pb-0"
          >
            <span class="text-sm font-medium text-gray-700">{{ provider.label }}</span>
            <span v-if="linkedProviderIds.includes(provider.id)" class="text-sm text-green-600">Connected</span>
            <Button
              v-else
              type="button"
              variant="outline"
              :disabled="isLinking[provider.id]"
              @click="connectProvider(provider.id)"
            >
              {{ isLinking[provider.id] ? 'Connecting...' : 'Connect' }}
            </Button>
          </div>
        </div>
      </Card>
    </div>

    <Toast ref="toastRef" />
  </section>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'

import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Input, PasswordInput } from '@/components/ui/input'
import Toast from '@/components/ui/custom/Toast/Toast.vue'
import { useToast } from '@/composables/useToast'
import {
  changePassword,
  confirmEmailChange,
  deactivateAccount as deactivateAccountRequest,
  editUser,
  getUser,
  requestEmailChange,
} from '@/services/userServices'
import { getCurrentUser, logout } from '@/services/authService'
import { auth, googleProvider, githubProvider, signInWithPopup, linkWithPopup } from '@/services/firebase'
import { getLinkedProviderIds, getSocialAuthErrorFeedback } from '@/utils/socialAuthFeedback'

const router = useRouter()
const { success, showError, toastRef } = useToast()

const user = ref(null)
const isLoading = ref(true)
const isSaving = ref(false)
const isDeactivating = ref(false)
const formError = ref('')
const form = reactive({
  firstName: '',
  lastName: '',
  gender: 2,
})

const passwordForm = reactive({ currentPassword: '', newPassword: '' })
const passwordError = ref('')
const isChangingPassword = ref(false)

const emailForm = reactive({ newEmail: '', currentPassword: '', code: '' })
const emailError = ref('')
const emailChangePending = ref(false)
const isRequestingEmailChange = ref(false)
const isConfirmingEmailChange = ref(false)

const genderOptions = [
  { value: 0, label: 'Male' },
  { value: 1, label: 'Female' },
  { value: 2, label: 'Other' },
]

const providerOptions = [
  { id: 'google.com', label: 'Google' },
  { id: 'github.com', label: 'GitHub' },
]

const linkedProviderIds = ref([])
const identityVerified = ref(false)
const linkedAccountsError = ref('')
const isLinking = reactive({ 'google.com': false, 'github.com': false })
const isVerifying = reactive({ 'google.com': false, 'github.com': false })

const providerInstance = (providerId) => (providerId === 'google.com' ? googleProvider : githubProvider)

const timeSinceRegistration = computed(() => {
  if (!user.value?.registrationDate) return 'recently joined'

  const elapsedDays = Math.max(
    0,
    Math.floor((Date.now() - new Date(user.value.registrationDate).getTime()) / 86400000),
  )

  if (elapsedDays >= 365) {
    const years = Math.floor(elapsedDays / 365)
    return `${years} ${years === 1 ? 'year' : 'years'} ago`
  }

  if (elapsedDays >= 30) {
    const months = Math.floor(elapsedDays / 30)
    return `${months} ${months === 1 ? 'month' : 'months'} ago`
  }

  return `${elapsedDays} ${elapsedDays === 1 ? 'day' : 'days'} ago`
})

const loadProfile = async () => {
  const storedUser = getCurrentUser()

  if (!storedUser?.id) {
    formError.value = 'Your session does not contain a valid user.'
    isLoading.value = false
    return
  }

  try {
    const currentUser = await getUser(storedUser.id)
    user.value = currentUser
    form.firstName = currentUser.firstName ?? ''
    form.lastName = currentUser.lastName ?? ''
    form.gender = Number(currentUser.gender ?? 2)
    refreshLinkedAccountsState()
  } catch (error) {
    showError('Profile error', error?.response?.data?.message || 'Unable to load your profile.')
  } finally {
    isLoading.value = false
  }
}

const validateForm = () => {
  if (!form.firstName.trim() || !form.lastName.trim()) {
    return 'First name and last name are required.'
  }

  if (![0, 1, 2].includes(Number(form.gender))) {
    return 'Select a valid gender.'
  }

  return ''
}

const saveProfile = async () => {
  formError.value = validateForm()
  if (formError.value || !user.value) return

  isSaving.value = true
  try {
    const updatedUser = await editUser(user.value.id, {
      firstName: form.firstName.trim(),
      lastName: form.lastName.trim(),
      gender: Number(form.gender),
      image: user.value.image ?? null,
    })

    user.value = updatedUser
    localStorage.setItem('user', JSON.stringify(updatedUser))
    window.dispatchEvent(new Event('user-updated'))
    success('Profile updated', 'Your personal information was saved.')
  } catch (error) {
    showError('Update failed', error?.response?.data?.message || 'Unable to update your profile.')
  } finally {
    isSaving.value = false
  }
}

const deactivateAccount = async () => {
  if (!user.value) return

  const confirmed = window.confirm(
    'Are you sure you want to deactivate your account? You will have 30 days to recover it before your data is permanently anonymized.',
  )
  if (!confirmed) return

  const currentPassword = window.prompt('Enter your current password to confirm deactivation:')
  if (!currentPassword) return

  isDeactivating.value = true
  try {
    await deactivateAccountRequest(user.value.id, { currentPassword })
    logout()
    await router.push({ name: 'landing' })
  } catch (error) {
    showError('Deactivation failed', error?.response?.data?.message || 'Unable to deactivate your account.')
  } finally {
    isDeactivating.value = false
  }
}

const submitPasswordChange = async () => {
  passwordError.value = ''
  if (!passwordForm.currentPassword || passwordForm.newPassword.length < 8) {
    passwordError.value = 'Enter your current password and a new password with at least 8 characters.'
    return
  }

  isChangingPassword.value = true
  try {
    await changePassword(user.value.id, {
      currentPassword: passwordForm.currentPassword,
      newPassword: passwordForm.newPassword,
    })
    passwordForm.currentPassword = ''
    passwordForm.newPassword = ''
    success('Password updated', 'Your password was changed successfully.')
  } catch (error) {
    passwordError.value = error?.response?.data?.message || 'Unable to change your password.'
  } finally {
    isChangingPassword.value = false
  }
}

const submitEmailChangeRequest = async () => {
  emailError.value = ''
  if (!emailForm.newEmail.trim() || !emailForm.currentPassword) {
    emailError.value = 'Enter the new email and your current password.'
    return
  }

  isRequestingEmailChange.value = true
  try {
    await requestEmailChange(user.value.id, {
      newEmail: emailForm.newEmail.trim(),
      currentPassword: emailForm.currentPassword,
    })
    emailChangePending.value = true
    success('Confirmation code sent', 'Check the new email address for a confirmation code.')
  } catch (error) {
    emailError.value = error?.response?.data?.message || 'Unable to request the email change.'
  } finally {
    isRequestingEmailChange.value = false
  }
}

const submitEmailChangeConfirmation = async () => {
  emailError.value = ''
  if (!emailForm.code.trim()) {
    emailError.value = 'Enter the confirmation code.'
    return
  }

  isConfirmingEmailChange.value = true
  try {
    await confirmEmailChange(user.value.id, emailForm.code.trim())
    emailChangePending.value = false
    emailForm.newEmail = ''
    emailForm.currentPassword = ''
    emailForm.code = ''
    success('Email changed', 'Sign in again with your new email address.')
    logout()
    await router.push({ name: 'login' })
  } catch (error) {
    emailError.value = error?.response?.data?.message || 'Unable to confirm the email change.'
  } finally {
    isConfirmingEmailChange.value = false
  }
}

const refreshLinkedAccountsState = () => {
  identityVerified.value = auth.currentUser?.email?.toLowerCase() === user.value?.email?.toLowerCase()
  linkedProviderIds.value = identityVerified.value
    ? getLinkedProviderIds(auth.currentUser, user.value.email)
    : []
}

const connectProvider = async (providerId) => {
  linkedAccountsError.value = ''
  isLinking[providerId] = true
  try {
    const result = await linkWithPopup(auth.currentUser, providerInstance(providerId))
    linkedProviderIds.value = result.user.providerData.map((provider) => provider.providerId)
    const label = providerOptions.find((option) => option.id === providerId)?.label ?? providerId
    success('Account connected', `${label} is now linked to your account.`)
  } catch (error) {
    const feedback = getSocialAuthErrorFeedback(error, 'Unable to connect this account')
    // TODO(temp diagnostic): remove the error.code suffix once the root cause is confirmed.
    if (feedback) linkedAccountsError.value = `${feedback.description ?? feedback.title}${error?.code ? ` (${error.code})` : ''}`
  } finally {
    isLinking[providerId] = false
  }
}

const verifyIdentity = async (providerId) => {
  linkedAccountsError.value = ''
  isVerifying[providerId] = true
  try {
    const result = await signInWithPopup(auth, providerInstance(providerId))
    if (result.user.email?.toLowerCase() !== user.value?.email?.toLowerCase()) {
      linkedAccountsError.value = "That account doesn't match your profile email."
      return
    }
    identityVerified.value = true
    linkedProviderIds.value = result.user.providerData.map((provider) => provider.providerId)
  } catch (error) {
    const feedback = getSocialAuthErrorFeedback(error, 'Unable to verify your identity')
    // TODO(temp diagnostic): remove the error.code suffix once the root cause is confirmed.
    if (feedback) linkedAccountsError.value = `${feedback.description ?? feedback.title}${error?.code ? ` (${error.code})` : ''}`
  } finally {
    isVerifying[providerId] = false
  }
}

onMounted(loadProfile)
</script>
