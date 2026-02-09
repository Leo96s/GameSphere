import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import * as z from 'zod'

// Mudamos a tipagem para z.ZodAny ou z.Schema para aceitar ZodEffects (refine)
export function useAppForm<T extends z.Schema<any>>(schema: T) {

  if (!schema) {
    console.error("useAppForm: Schema is undefined!");
    return { form: {} as any, clearError: () => {} };
  }

  // 1. Lógica robusta para extrair o 'shape'
  // Se for um ZodEffects (refine), o shape está dentro de _def.schema.shape
  // Se for um ZodObject, está em .shape
  const internalSchema = (schema instanceof z.ZodEffects)
    ? schema._def.schema
    : schema;

  const shape = (internalSchema as any).shape;

  // 2. Fallback caso o shape ainda não exista (segurança extra)
  if (!shape) {
    console.warn("useAppForm: Could not extract shape from schema.");
  }

  const initialValues = shape ? Object.keys(shape).reduce((acc, key) => {
    let field = shape[key];

    if (field instanceof z.ZodEffects) {
      field = field._def.schema;
    }

    if (field instanceof z.ZodBoolean) {
      acc[key] = false;
    } else {
      acc[key] = '';
    }

    return acc;
  }, {} as any) : {};

  const form = useForm({
    validationSchema: toTypedSchema(schema),
    initialValues,
    validateOnBlur: false,
    validateOnChange: false,
  })

  const clearError = (fieldName: string) => {
    form.setFieldError(fieldName, undefined)
  }

  return { form, clearError }
}