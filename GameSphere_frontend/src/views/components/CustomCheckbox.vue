<template>
    <div class="form-check mt-3">
      <input type="checkbox" class="form-check-input" :id="id" v-model="checkedValue" />
      <label class="form-check-label" :for="id">
        <slot></slot>
      </label>
    </div>
  </template>
  
  <script>
  export default {
    props: {
      id: String,
      modelValue: Boolean,
    },
    computed: {
      checkedValue: {
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
  .form-check-label a {
    text-decoration: underline;
    color: #5e72e4;
  }
  </style>
  