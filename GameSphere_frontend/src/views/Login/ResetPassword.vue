<template>
    <section class="section section-shaped section-lg my-0">
      <div class="shape shape-style-1 bg-gradient-default"></div>
      <div class="container pt-lg-md">
        <div class="row justify-content-center">
          <div class="col-lg-5">
            <!-- Card -->
            <div class="card border-0 shadow">
              <!-- Card Body -->
              <div class="card-body px-lg-5">
                <div class="text-center text-muted mb-4">
                  <small>Reset your password</small>
                </div>
                <form @submit.prevent="resetPassword">
                  <!-- Code Input -->
                  <CustomField label="Code" type="text" placeholder="Enter the code with 6 digits" v-model="code"/>
                  
                  <!-- Password Input -->
                  <CustomField label="Password" type="password" placeholder="Enter your new password" v-model="password" />
  
                  <!-- Password Confirmation Input -->
                  <CustomField label="Confirm Password" type="password" placeholder="Enter again your new password" v-model="password2"/>
  
                  <!-- Submit Button -->
                  <CustomButton label="Reset Password" type="submit" variant="primary" :disabled="isSubmitting" />
                </form>
  
                <!-- Error message -->
                <div v-if="error" class="alert alert-danger text-center">
                  {{ error }}
                </div>
  
                <!-- Success message -->
                <div v-if="success" class="alert alert-success text-center">
                  {{ success }}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  </template>
  
  
  <script>
import CustomButton from "../components/CustomButton.vue";
import CustomField from "../components/CustomField.vue";
import { resetPassword, validateResetCodeRequest } from "../../services/authService";

export default {
  name: "SentCodeCard",
  components: {
    CustomField,
    CustomButton
  },
  mounted() {
    const storedEmail= localStorage.getItem("email");
    console.log("Email recuperado do localStorage:", storedEmail);
    if (storedEmail) {
      this.email = JSON.parse(storedEmail);
    }
  },
  data() {
    return {
      code: "",
      password: "",
      password2: "",
      error: null,
      success: null,
      isSubmitting: false
    };
  },
  methods: {
    async resetPassword() {
      try {
        // Zera mensagens anteriores
        this.error = null;
        this.success = null;
        this.isSubmitting = true;

        // Valida os campos obrigatórios
        if (!this.code || !this.password || !this.password2) {
          this.error = "All fields are required.";
          this.isSubmitting = false;
          return;
        }

        // Verifica se as senhas são iguais
        if (this.password !== this.password2) {
          this.error = "Passwords do not match.";
          this.isSubmitting = false;
          return;
        }
        console.log(this.email, this.code);

        // Verifica o código de redefinição
        const verifyCode = await validateResetCodeRequest(this.email, this.code);

        if (verifyCode != false) {
          this.error = "Invalid or expired reset code.";
          this.isSubmitting = false;
          return;
        }

        // Realiza a redefinição da senha
        const response = await resetPassword(this.email, this.code, this.password);

        if (response != false) {
          this.error = "Error resetting password.";
        } else {
          this.success = "Password successfully changed.";
          setTimeout(() => {
            this.$router.push('/login').then(() => {
              location.reload();
            });
          }, 500);
        }

        this.isSubmitting = false;
      } catch (err) {
        this.error = err.response?.data?.message || "An error occurred during password reset.";
        console.error(err);
        this.isSubmitting = false;
      }
    }
  }
};
</script>
  
  <style>
  /* Estilo básico */
  .remember {
    padding-top: 10px;
    display: flex;
  
  }
  
  .forget-password {
    margin-left: auto;
    text-align: right;
  }
  
  .custom-control-label {
    padding-left: 10px;
  }
  
  .section {
    position: relative;
    display: flex;
    align-items: center;
  }
  
  .shape {
    position: absolute;
    width: 100%;
    height: 100%;
    overflow: hidden;
    z-index: 1;
  }
  
  .shape span {
    display: block;
    position: absolute;
    border-radius: 50%;
    background: rgba(255, 255, 255, 0.15);
  }
  
  .btn-icon {
    width: 200px;
    /* Define um tamanho fixo para os botões */
    height: 50px;
    align-items: center;
    justify-content: center;
    border-radius: 100%;
    /* Torna os botões arredondados */
    padding: 5px;
    background-color: #f1f1f1;
  }
  
  .icon-img {
    width: 24px;
    /* Define um tamanho menor para os ícones */
    height: 24px;
  }
  
  .btn-icon img.icon {
    width: 20px;
    margin-right: 8px;
  }
  
  .card {
    position: relative;
    z-index: 2;
  }
  </style>