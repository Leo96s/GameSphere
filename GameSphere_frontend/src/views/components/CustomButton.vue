<template>
    <div class="text-center">
      <button 
        :type="type"
        class="btn my-4"
        :class="buttonClass"
        :disabled="disabled"
        @click="handleClick"
      >
        {{ label }}
      </button>
    </div>
  </template>
  
  <script>
  export default {
    props: {
      label: String,
      type: {
        type: String,
        default: "button"
      },
      variant: {
        type: String,
        default: "primary"
      },
      disabled: Boolean
    },
    computed: {
      buttonClass() {
        return `btn-${this.variant}`;
      }
    },
    methods: {
      handleClick() {
        if (!this.disabled) {
          this.$emit("click");
        }
      }
    }
  };
  </script>
  
  <style scoped>
  .btn {
    padding: 10px 20px;
    font-size: 16px;
  }
  </style>