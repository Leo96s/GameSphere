// @/utils/password.ts
export type Strength = 'Weak' | 'Medium' | 'Strong'

export function getPasswordStrength(password: string): Strength {
  if (!password || password.length < 6) return 'Weak'

  const hasNumbers = /\d/.test(password)
  const hasLetters = /[a-zA-Z]/.test(password)
  const hasSpecial = /[\W_]/.test(password)

  if (password.length >= 10 && hasNumbers && hasLetters && hasSpecial) return 'Strong'
  if (password.length >= 8 && hasNumbers && hasLetters) return 'Medium'

  return 'Weak'
}