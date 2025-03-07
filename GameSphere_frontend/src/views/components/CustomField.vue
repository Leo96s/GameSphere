<!-- eslint-disable vue/no-mutating-props -->
<template>
    <div class="form-group mb-3">
      <label v-if="label">{{ label }}</label>
      <input
        :type="type"
        :placeholder="placeholder"
        :value="modelValue"
        @input="$emit('update:modelValue', $event.target.value)"
        class="form-control"
        required
      />
    </div>
  </template>
  
  <script>
  export default {
    name: "CustomField",
    props: {
      label: String,
      type: {
        type: String,
        default: "text",
      },
      placeholder: String,
      modelValue: String,
    },
    emits: ["update:modelValue"],
  };
  </script>
  
  <style scoped>
  .form-group {
    margin-bottom: 1rem;
  }
  </style>