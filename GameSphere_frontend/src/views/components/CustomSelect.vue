<template>
    <div class="form-group mb-3">
      <label>{{ label }}</label>
      <select class="form-control" v-model="selectedValue">
        <option v-for="option in options" :key="option.value" :value="option.value">
          {{ option.label }}
        </option>
      </select>
    </div>
  </template>
  
  <script>
  export default {
    props: {
      label: String,
      modelValue: [String, Number],
      options: Array,
    },
    computed: {
      selectedValue: {
        get() {
          return this.modelValue;
        },
        set(value) {
          this.$emit('update:modelValue', value);
        }
      }
    }
  };
  </script>
  
  <style scoped>
  </style>
  