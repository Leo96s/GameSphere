# 🍞 Toast

The `Toast` component provides non-intrusive feedback to the user following an action.

## 🛠 Usage

This component uses `defineExpose`, meaning it must be accessed via a **Vue template ref**.

### 1. Component Declaration
Place the component at a global level (usually `App.vue` or a main layout):

```vue
<Toast ref="toastRef" />

```

### 2. Triggering Notifications

Call the `showToast` method from your logic:

```typescript
const toastRef = ref();

toastRef.value.showToast({
  title: "Operation Successful",
  description: "User profile updated.",
  variant: "success",
  duration: 4000
});

```

## 🎨 Properties & Variants

| Variant | Tailwind Border | Use Case |
| --- | --- | --- |
| `success` | `border-primary/30` | Successful login, data save, or registration. |
| `error` | `border-destructive/40` | API failures, validation errors, or timeouts. |

## 🔍 Interaction Testing

You can preview animations and timing in our [Storybook](https://www.google.com/search?q=http://localhost:6006/%3Fpath%3D/story/components-ui-toast--success).
