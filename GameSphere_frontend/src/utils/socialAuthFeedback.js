/**
 * Maps a Firebase Auth error to user-facing toast feedback.
 * Returns null when the error should be silently ignored (e.g. the user
 * closed the popup themselves) — callers should skip showing a toast then.
 */
export function getSocialAuthErrorFeedback(error, fallbackTitle) {
  switch (error?.code) {
    case "auth/popup-closed-by-user":
    case "auth/cancelled-popup-request":
      return null
    case "auth/account-exists-with-different-credential":
      return {
        title: fallbackTitle,
        description:
          "This email already has an account with a different sign-in method (e.g. Google). Sign in with that method, then connect this one from your Profile page.",
      }
    case "auth/credential-already-in-use":
      return {
        title: fallbackTitle,
        description: "This account is already connected to a different user.",
      }
    default:
      return { title: fallbackTitle }
  }
}

/**
 * Returns the Firebase provider ids (e.g. "google.com", "github.com") linked
 * to firebaseUser, but only when its email matches expectedEmail — guards
 * against showing linked-provider state from an unrelated cached Firebase
 * session.
 */
export function getLinkedProviderIds(firebaseUser, expectedEmail) {
  if (!firebaseUser || !expectedEmail) return []
  if (firebaseUser.email?.toLowerCase() !== expectedEmail.toLowerCase()) return []
  return firebaseUser.providerData.map((provider) => provider.providerId)
}
