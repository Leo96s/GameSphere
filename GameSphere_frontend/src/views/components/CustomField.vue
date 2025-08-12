<!-- eslint-disable vue/no-mutating-props -->
<template>
  <div class="form-group mb-3">
    <label>{{ label }}</label>
    <div class="input-group">
      <!-- Campo de input -->
      <input
        :type="inputType"
        class="form-control"
        :placeholder="placeholder"
        v-model="inputValue"
      />
      <!-- Ícone de visibilidade da senha, posicionado à direita -->
      <span v-if="isPassword" class="password-toggle" @click="togglePassword">
        <i :class="passwordVisible ? 'fa fa-eye-slash' : 'fa fa-eye'"></i>
      </span>
    </div>
  </div>
</template>

<script>
export default {
  props: {
    label: String,
    type: {
      type: String,
      default: "text"
    },
    placeholder: String,
    modelValue: String,
  },
  data() {
    return {
      passwordVisible: false
    };
  },
  computed: {
    inputValue: {
      get() {
        return this.modelValue;
      },
      set(value) {
        this.$emit("update:modelValue", value);
      }
    },
    inputType() {
      return this.isPassword && !this.passwordVisible ? "password" : "text";
    },
    isPassword() {
      return this.type === "password";
    }
  },
  methods: {
    togglePassword() {
      this.passwordVisible = !this.passwordVisible;
    }
  }
};
</script>

<style scoped>
.input-group {
  position: relative;
  display: flex;
  align-items: center;
}

.password-toggle {
  position: absolute;
  right: 10px; /* Posiciona o ícone à direita */
  top: 50%;
  transform: translateY(-50%); /* Centraliza o ícone verticalmente */
  cursor: pointer;
  font-size: 1.2rem;
  color: #6c757d; /* Cor do ícone */
}

.password-toggle:hover {
  color: #000; /* Cor quando o ícone é hover */
}

.form-control {
  padding-right: 35px; /* Adiciona espaço à direita para o ícone */
}
</style>








