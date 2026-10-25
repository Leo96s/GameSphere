import { useForm, type TypedSchema } from 'vee-validate'
import * as z from 'zod'

// Adaptador zod -> vee-validate. Substitui o @vee-validate/zod, que só suporta zod 3.
// Os erros são agrupados por campo, com caminhos aninhados no formato do vee-validate (a.b[0]).
export function toTypedSchema<T extends z.ZodType>(schema: T): TypedSchema<z.input<T>, z.output<T>> {
  return {
    __type: 'VVTypedSchema',
    async parse(values) {
      const result = await schema.safeParseAsync(values)
      if (result.success) {
        return { value: result.data, errors: [] }
      }

      const errors: Record<string, { path: string; errors: string[] }> = {}
      for (const issue of result.error.issues) {
        const path = issue.path
          .map((segment, index) => typeof segment === 'number' ? `[${segment}]` : `${index ? '.' : ''}${String(segment)}`)
          .join('')
        errors[path] ??= { path, errors: [] }
        errors[path].errors.push(issue.message)
      }

      return { errors: Object.values(errors) }
    },
  }
}

export function useAppForm<T extends z.ZodType>(schema: T) {

  if (!schema) {
    console.error("useAppForm: Schema is undefined!");
    return { form: {} as any, clearError: () => {} };
  }

  // 1. Extrair o 'shape' para os valores iniciais
  // No zod 4 o refine não embrulha o schema, por isso um objecto refinado continua a ser ZodObject
  const shape = schema instanceof z.ZodObject ? schema.shape : undefined;

  // 2. Fallback caso o shape não exista (segurança extra)
  if (!shape) {
    console.warn("useAppForm: Could not extract shape from schema.");
  }

  const initialValues = shape ? Object.keys(shape).reduce((acc, key) => {
    if (shape[key] instanceof z.ZodBoolean) {
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
