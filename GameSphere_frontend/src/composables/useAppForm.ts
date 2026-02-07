import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import * as z from 'zod'

export function useAppForm<T extends z.ZodObject<any>>(schema: T) {
  const shape = schema.shape;

  // Criamos um objeto onde cada chave do Zod começa com uma string vazia (ou false se preferires)
  const initialValues = Object.keys(shape).reduce((acc, key) => {
    let field = shape[key];
  // ele torna-se um ZodEffects. Precisamos de extrair o esquema original.
    if (field instanceof z.ZodEffects) {
      field = field._def.schema;
    }

    // Agora o instanceof z.ZodBoolean já vai funcionar!
    if (field instanceof z.ZodBoolean) {
      acc[key] = false;
    } else {
      acc[key] = '';
    }

    return acc;
  }, {} as any);

  const form = useForm({
    validationSchema: toTypedSchema(schema),
    initialValues,
    validateOnBlur: false,
    validateOnChange: false,
  })

  // Helper para limpar erro
  const clearError = (fieldName: keyof z.infer<T>) => {
    form.setFieldError(fieldName as string, undefined)
  }

  return { form, clearError }
}