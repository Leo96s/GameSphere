# 📝 User Registration

The Registration view is the primary gateway for new users to join the GameSphere ecosystem. It is built for maximum conversion using both social and manual flows.

## 🔄 Registration Workflow



### 1. Manual Flow (Form-based)
- **Data Mapping:** Uses the `User` model to encapsulate form data.
- **Validation:** Powered by `vee-validate` and `zod`.
- **UX Features:** - **Password Strength:** Real-time feedback using `getPasswordStrength`.
    - **Gender Select:** Controlled via a custom Shadcn UI `Select` component.
    - **Policy Enforcement:** Registration is blocked unless `acceptedPolicy` is checked.

### 2. Social Flow (OAuth)
- **Firebase Bridge:** Uses `signInWithPopup` to authenticate with Google or GitHub.
- **Data Normalization:** - Google `displayName` is split into `firstName` and `lastName`.
    - GitHub users are assigned a generic last name.
    - A random 8-character password is generated for internal system compatibility.
- **Automatic Sync:** Calls the `createUser` service immediately after OAuth success.

## 🛠 Technical Configuration

### Zod Schema
```typescript
const registerSchema = z.object({
  firstName: z.string().min(1, "First name is required"),
  lastName: z.string().min(1, "Last name is required"),
  email: z.string().min(1, "Email is required").email("Invalid email format"),
  password: z.string().min(8, "Minimum 8 characters"),
  gender: z.string().min(1, "Select a gender"),
  acceptedPolicy: z.boolean().refine(v => v, "You must accept the terms"),
});

```

### Form Dependencies

* `useAppForm`: Custom composable to handle field errors and state.
* `useToast`: Provides the `success()` and `showError()` methods for UI feedback.
* `User Model`: Ensures the payload sent to the API matches the backend DTO.

## 🔍 Interaction Testing

Preview how the registration card handles different screen sizes and validation states in the [Storybook Registration Suite](https://www.google.com/search?q=http://localhost:6006/%3Fpath%3D/story/views-auth-register--default).

### Comparison: Manual vs. Social Registration

| Feature | Manual Registration | Social Registration |
| :--- | :--- | :--- |
| **Data Source** | User Input | Provider Profile (Google/GitHub) |
| **Password** | User Defined | Auto-generated (8 chars) |
| **Validation** | Zod (All fields) | Partial (Backend handled) |
| **Initial Role** | Standard User | Standard User |
| **Avatar** | Default | Provider Photo (if implemented) |
