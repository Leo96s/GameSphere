# 🔑 Login Page

The entry point for returning users. Supports multiple authentication methods.

## Features
- **Social Login:** Google and GitHub integration via Firebase.
- **Form Validation:** Strict email and password (min 8 chars) validation via Zod.
- **Persistence:** Emits `user-logged-in` event and updates `localStorage`.
- **Feedback:** Uses the `Toast` component for success/error messages.

### Authentication Methods
1. **Traditional:** Standard Email/Password handled via `authService.login()`.
2. **Social (Google/GitHub):** - Step A: Firebase handles the popup and provider handshake.
   - Step B: We send the `uid` and `email` to our backend via `social_login()` to sync the local user profile.

### State Synchronization
The component triggers a `window.dispatchEvent(new Event("user-logged-in"))`. This is crucial because:
- It allows the **Header** to update without a full page refresh.
- It ensures cross-tab synchronization.

> [!TIP]
> Always use `useAppForm` to handle `clearError`. This prevents the UI from showing stale validation errors when a user starts re-typing.