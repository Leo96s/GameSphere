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
                <form @submit.prevent="sentCode">
                  <!-- Email Input -->
                  <div class="form-group mb-3">
                    <div class="input-group input-group-alternative">
                      <input type="email" class="form-control" placeholder="Email" v-model="email" required />
                    </div>
  
                    <!-- Submit Button -->
                    <div class="text-center">
                      <button type="submit" class="btn btn-primary my-4">
                        Submit
                      </button>
                    </div>
                  </div>
                </form>
                <div v-if="error" class="alert alert-danger text-center">
                  {{ error }}
                </div>
                <div v-if="success" class="alert alert-success text-center">
                  {{ success }}
                </div>
              </div>
            </div>
  
            <!-- Footer Links -->
            <div class="row mt-3">
              <div class="col-6">
                <a href="#" class="text-light">
                  <small>Forgot password?</small>
                </a>
              </div>
              <div class="col-6 text-right">
                <a href="#" class="text-light">
                  <small>Create new account</small>
                </a>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  </template>
  
  <script>
  import {sentResetCode } from "@/services/authService";
  
  export default {
    name: "SentCodeCard",
    data() {
      return {
      };
    },
    methods: {
      async sentCode() {
        try {
          // Zera mensagens anteriores
          this.error = null;
          this.success = null;
  
          // Valida os campos obrigatórios
          if (!this.email) {
            this.error = "All fields are required.";
            return;
          }
  
          const response = await sentResetCode(this.email);
  
          if (!response) {
            console.error("Erro: Falha ao enviar email.");
          }
  
          this.success = "Email sent!";
  
          setTimeout(() => {
            this.$router.push('/profile').then(() => {
              location.reload();
            });
          }, 500);
        } catch (err) {
          // Exibir mensagem de erro
          this.error = err.response?.data?.message || "An error occurred during registration.";
          console.error(err);
        }
      },
    },
      
  };
  
  </script>
  
  <style>
  /* Estilo básico */
  .remember {
    padding-top: 10px;
    display: flex;
  
  }
  
  .forget-password{
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
    width: 200px;  /* Define um tamanho fixo para os botões */
    height: 50px;
    align-items: center;
    justify-content: center;
    border-radius: 100%;  /* Torna os botões arredondados */
    padding: 5px;
    background-color: #f1f1f1;
  }
  
  .icon-img {
    width: 24px;  /* Define um tamanho menor para os ícones */
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