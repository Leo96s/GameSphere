<template>
    <section class="section section-shaped section-lg my-0">
      <div class="shape shape-style-1 bg-gradient-default"></div>
      <div class="container pt-lg-md">
        <div class="row justify-content-center">
          <div class="col-lg-5">
            <!-- Card -->
            <div class="card border-0 shadow">
              <!-- Card Header -->
              <div class="card-header bg-white pb-5">
                <div class="text-muted text-center mb-3">
                  <small>Sign in with</small>
                </div>
                <div class="btn-wrapper text-center">
                  <button class="btn btn-neutral btn-icon mb-2">
                    <img src="@/assets/logos/facebook.png" alt="Facebook" class="icon">
                  </button>
                  <button class="btn btn-neutral btn-icon">
                    <img src="@/assets/logos/google.png" alt="Google" class="icon">
                  </button>
                  <button class="btn btn-neutral btn-icon">
                    <img src="@/assets/logos/microsoft.png" alt="Microsoft" class="icon">
                  </button>
                </div>
              </div>

              <!-- Card Body -->
              <div class="card-body px-lg-5">
                <div class="text-center text-muted mb-4">
                  <small>Or sign in with credentials</small>
                </div>
                <form @submit.prevent="handleLogin">
                  <!-- Email Input -->
                  <div class="form-group mb-3">
                    <div class="input-group input-group-alternative">
                      <input
                        type="email"
                        class="form-control"
                        placeholder="Email"
                        v-model="email"
                        required
                      />
                    </div>

                  <!-- Password Input -->
                    <div class="input-group input-group-alternative py-3">
                      <input
                        type="password"
                        class="form-control"
                        placeholder="Password"
                        v-model="password"
                        required
                      />
                    </div>

                  <!-- Checkbox -->
                  <div class="remember">
                    <input
                      type="checkbox"
                      class="custom-control-input"
                      id="rememberMe"
                    />
                    <label
                      class="custom-control-label"
                      for="rememberMe"
                    >
                      Remember me
                    </label>
                  </div>

                  <!-- Submit Button -->
                  <div class="text-center">
                    <button
                      type="submit"
                      class="btn btn-primary my-4"
                    >
                      Sign In
                    </button>
                  </div>
                  </div>
                </form>
                <div v-if="error" class="alert alert-danger text-center">
                  {{error}}
                </div>
                <div v-if="success" class="alert alert-success text-center">
                  {{success}}
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
  import {login} from "@/services/authService";
  export default {
    name: "LoginCard",
    data() {
      return {
        email: "",
        password: "",
        error: null,
        success: null
      };
    },
    methods:{
      async handleLogin(){
        try {
          // Zera mensagens anteriores
          this.error = null;
          this.success = null;

          // Valida os campos obrigatórios
          if (!this.email || !this.password) {
            this.error = "All fields are required.";
            return ;
          }

          const response = await login(this.email, this.password);

          if (response.data) {
            localStorage.setItem("user", JSON.stringify(response.data));
          } else {
          console.error("Erro: Nenhum dado recebido do login.");
          }

          this.success = "Account created successfully!";
          console.log("User login-in:", response);

          setTimeout(() => {
            this.$router.push('/profile')
          }, 2000);
        }catch (err){
          // Exibir mensagem de erro
          this.error = err.response?.data?.message || "An error occurred during registration.";
          console.error(err);
        }
      }
    },
  };

  </script>

  <style>
  /* Estilo básico */
  .remember{
    padding-top: 10px;
  }

  .custom-control-label{
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

  .btn-icon img.icon {
    width: 20px;
    margin-right: 8px;
  }

  .card {
    position: relative;
    z-index: 2;
  }
  </style>

