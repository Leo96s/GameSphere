# 📧 Send Recovery Code

Starts the "Forgot Password" flow by identifying the user's email.

## Workflow
1. User enters registered email.
2. Call `sentResetCode` API.
3. Email is cached in `localStorage` for the next step.
4. Redirects to `/forgetPassword/resetPassword`.