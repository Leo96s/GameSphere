# 🔄 Reset Password

Allows users to update their credentials using a verification code.

## Key Logic
- **Password Strength:** Real-time feedback using `getPasswordStrength` utility.
- **Confirmation:** Zod `refine` check to ensure both password fields match.
- **Pre-requisite:** Expects an `email` item in `localStorage` from the previous step.

## Security UX
We implement a **Password Strength Meter** based on entropy and character variety.

### Validation Rules (Zod)
- **Code:** Must be exactly 6 characters (matches backend SMTP generator).
- **Password:** Minimum 8 characters.
- **Refinement:** The `confirmPassword` field must strictly match the `password` field or the form will block submission.

### Recovery Workflow
1. Retrieve `email` from `localStorage` (saved in the previous step).
2. Send `email`, `code`, and `newPassword` to `authService.resetPassword()`.
3. If successful, we clear the temporary `email` storage to prevent stale data.