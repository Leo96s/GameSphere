<template>
    <div class="card-body px-lg-5">
      <div class="text-center text-muted mb-4">
        <small>Or sign in with credentials</small>
      </div>
      <form @submit.prevent="handleLogin">
        <!-- Usando o CustomField.vue para cada campo -->
        <CustomField
          v-for="(field, index) in fields"
          :key="index"
          :label="field.label"
          :type="field.type"
          :placeholder="field.placeholder"
          v-model="formData[field.model]"
        />
  
        <!-- Checkbox opcional -->
        <div v-if="showCheckbox" class="remember">
          <input type="checkbox" class="custom-control-input" id="rememberMe" />
          <label class="custom-control-label" for="rememberMe">
            {{ checkboxText }}
          </label>
        </div>
  
        <!-- Submit Button -->
        <div class="text-center">
          <button type="submit" class="btn btn-primary my-4">
            {{ submitButtonText }}
          </button>
        </div>
      </form>
    </div>
  </template>
  
  <script>
  import CustomField from "@/components/CustomField.vue"; // Importando o campo dinâmico
  
  export default {
    components: {
      CustomField
    },
    props: {
      fields: {
        type: Array,
        required: true
      },
      showCheckbox: {
        type: Boolean,
        default: false
      },
      checkboxText: {
        type: String,
        default: "Remember me"
      },
      submitButtonText: {
        type: String,
        default: "Sign In"
      }
    },
    data() {
      return {
        formData: {}
      };
    },
    created() {
      this.fields.forEach(field => {
        this.formData[field.model] = "";
      });
    },
    methods: {
      handleLogin() {
        console.log("Form Data:", this.formData);
      }
    }
  };
  </script>
  
  <style scoped>
  /* Adicione estilos personalizados se necessário */
  </style>