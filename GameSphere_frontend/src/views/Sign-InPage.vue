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
              <div class="btn-wrapper text-center space-x-4">
                <div class="text-center my-3">
                  <b-button @click="signinWithGoogle" title="Google" class="btn-icon mx-2" variant="light">
                    <img src="@/assets/logos/google.png" alt="Google" class="icon-img">
                  </b-button>

                  <b-button @click="signinWithGithub" title="Github" class="btn-icon" variant="dark">
                    <img src="@/assets/logos/github.png" alt="Github" class="icon-img">
                  </b-button>
                </div>
              </div>
            </div>

            <!-- Card Body -->
            <div class="card-body px-lg-5">
              <div class="text-center text-muted mb-4">
                <small>Or sign in with credentials</small>
              </div>
              <form @submit.prevent="handleLogin">
                <!-- Campos de Input usando CustomField -->
                <CustomField
                  label="Email"
                  type="email"
                  placeholder="Enter your email"
                  v-model="email"
                />

                <CustomField
                  label="Password"
                  type="password"
                  placeholder="Enter your password"
                  v-model="password"
                />

                <!-- Checkbox -->
                <div class="remember">
                  <input type="checkbox" class="custom-control-input" id="rememberMe" />
                  <label class="custom-control-label" for="rememberMe">
                    Remember me
                  </label>
                  <a href="/forgetPassword/sentCode" class="forget-password">Forget your password?</a>
                </div>

                <!-- Submit Button -->
                <div class="text-center">
                  <button type="submit" class="btn btn-primary my-4">
                    Sign In
                  </button>
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
import CustomField from "@/views/components/CustomField.vue";
import { login, social_login } from "@/services/authService";
import { auth, githubProvider, googleProvider, signInWithPopup } from "@/services/firebase";

export default {
  name: "LoginCard",
  components: {
    CustomField
  },
  data() {
    return {
      email: "",
      password: "",
      error: null,
      success: null
    };
  },
  methods: {
    async handleLogin() {
      try {
        this.error = null;
        this.success = null;

        if (!this.email || !this.password) {
          this.error = "All fields are required.";
          return;
        }

        const response = await login(this.email, this.password);

        if (response.data) {
          localStorage.setItem("user", JSON.stringify(response.data));
        } else {
          console.error("Erro: Nenhum dado recebido do login.");
        }

        this.success = "Login successfully!";
        console.log("User login-in:", response);

        setTimeout(() => {
          this.$router.push('/profile').then(() => {
            location.reload();
          });
        }, 500);
      } catch (err) {
        this.error = err.response?.data?.message || "An error occurred during registration.";
        console.error(err);
      }
    },
    async signinWithGoogle() {
      try {
        const result = await signInWithPopup(auth, googleProvider);
        const googleUser = result.user;

        const response = await social_login(googleUser.uid, googleUser.email);

        if (response) {
          localStorage.setItem("user", JSON.stringify(response.user));
          this.user = response.user;
        } else {
          console.error("Erro: Nenhum dado recebido do login.");
        }

        this.success = "Login successfully!";
        console.log("User sign-in:", response);

        setTimeout(() => {
          this.$router.push("/profile").then(() => {
            location.reload();
          });
        }, 500);
      } catch (err) {
        this.error = err.response?.data?.message || "An error occurred during registration.";
        console.error(err);
      }
    },
    async signinWithGithub() {
      try {
        const result = await signInWithPopup(auth, githubProvider);
        const githubUser = result.user;

        const response = await social_login(githubUser.uid, githubUser.email);

        if (response) {
          localStorage.setItem("user", JSON.stringify(response.user));
          this.user = response.user;
        } else {
          console.error("Erro: Nenhum dado recebido do login.");
        }

        this.success = "Login successfully!";
        setTimeout(() => {
          this.$router.push("/profile").then(() => {
            location.reload();
          });
        }, 500);
      } catch (err) {
        this.error = err.response?.data?.message || "An error occurred during registration.";
        console.error(err);
      }
    },
  },
};
</script>

<style>
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

.btn-icon {
  width: 200px;
  height: 50px;
  align-items: center;
  justify-content: center;
  border-radius: 100%;
  padding: 5px;
  background-color: #f1f1f1;
}

.icon-img {
  width: 24px;
  height: 24px;
}

.card {
  position: relative;
  z-index: 2;
}
</style>
