<template>
  <section class="section section-shaped section-lg my-0">
    <div class="shape shape-style-1 bg-gradient-default"></div>
    <div class="container pt-lg-md">
      <div class="row justify-content-center">
        <div class="col-lg-5">
          <div class="card shadow border-0">
            <!-- Card Header -->
            <div class="card-header bg-white pb-5 text-center">
              <div class="text-muted text-center mb-3">
                <small>Register with</small>
              </div>
              <div class="btn-wrapper text-center">
                <button class="btn btn-neutral btn-icon mb-2">
                  <img src="@/assets/logos/facebook.png" alt="Facebook" class="icon">
                </button>
                <button @click="registerWithGoogle" class="btn btn-neutral btn-icon" >
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
                <small>Or register with your credentials</small>
              </div>
              <form @submit.prevent="handleRegister">
                <div class="form-group mb-3">
                  <!-- Name Input -->
                  <div class="input-group input-group-alternative py-3">
                    <input
                      type="text"
                      class="form-control"
                      placeholder="firstName"
                      aria-label="firstName"
                      v-model="firstName"
                    />
                  </div>
                  <div class="input-group input-group-alternative py-3">
                    <input
                      type="text"
                      class="form-control"
                      placeholder="lastName"
                      aria-label="lastName"
                      v-model="lastName"
                    />
                  </div>

                  <div class="input-group input-group-alternative py-3">
                    <select
                      class="form-control"
                      aria-label="gender"
                      v-model="gender"
                    >
                      <option value="" disabled selected>Gender</option>
                      <option :value="Gender.Male">Male</option>
                      <option :value="Gender.Female">Female</option>
                      <option :value="Gender.Other">Other</option>
                    </select>
                  </div>

                  <!-- Email Input -->
                  <div class="input-group input-group-alternative py-2">
                    <input
                      type="email"
                      class="form-control"
                      placeholder="Email"
                      aria-label="Email"
                      v-model="email"
                    />
                  </div>

                  <!-- Password Input -->
                  <div class="input-group input-group-alternative py-2">
                    <input
                      type="password"
                      class="form-control"
                      placeholder="Password"
                      aria-label="Password"
                      v-model="password"
                    />
                  </div>

                  <!-- Password Strength -->
                  <div class="text-muted font-italic">
                    <small>
                      password strength:
                      <span class="text-success font-weight-700">strong</span>
                    </small>
                  </div>

                  <!-- Privacy Policy Checkbox -->
                  <div class="form-check mt-3">
                    <input type="checkbox" class="form-check-input" id="privacyPolicy" v-model="acceptedPolicy"/>
                    <label class="form-check-label" for="privacyPolicy">
                      I agree with the <a href="#">Privacy Policy</a>
                    </label>
                  </div>

                  <!-- Submit Button -->
                  <div class="text-center">
                    <button type="submit" class="btn btn-primary my-4" :disabled="!acceptedPolicy">
                      Create account
                    </button>
                  </div>
                </div>
              </form>
              <!-- Feedback Messages -->
              <div v-if="error" class="alert alert-danger text-center">
                {{error}}
              </div>
              <div v-if="success" class="alert alert-success text-center">
                {{success}}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script>
import {createUser} from "@/services/userServices";
import User from "@/models/User";
import {Gender} from "@/enums/Gender";
import { useRouter } from 'vue-router';
import { auth, provider, signInWithPopup } from "@/services/firebase";

export default {
  name: "RegisterSection",
  computed: {
    Gender() {
      return Gender
    }
  },
  data() {
    return {
      firstName: "",
      lastName: "",
      gender: "",
      email: "",
      password: "",
      acceptedPolicy: false,
      error: null,
      success: null,
    };
  },
  methods: {
    async handleRegister() {
      try {
        // Zera mensagens anteriores
        this.error = null;
        this.success = null;
        console.log(this.firstName, this.lastName, this.gender, this.email, this.password)
        // Valida os campos obrigatórios

        if (!this.firstName || !this.lastName || this.gender || !this.email || !this.password) {
          this.error = "All fields are required.";
          return ;
        }

        // Criar objeto `User` com todos os campos necessários
        const newUser = new User({
          firstName: this.firstName,
          lastName: this.lastName,
          email: this.email,
          gender: this.gender,
          password: this.password
        });
        // Enviar os dados ao backend
        const response = await createUser(newUser);
        // Exibir mensagem de sucesso
        this.success = "Account created successfully!";
        console.log("User created:", response);

        const router = useRouter();

        setTimeout(() => {
          console.log("Redirecionando para /login");
          router.push("/login");
        },2000);
      } catch (err) {
        // Exibir mensagem de erro
        this.error = err.response?.data?.message || "An error occurred during registration.";
        console.error(err);
      }
    },
    async registerWithGoogle(){
      try{
        const result = await signInWithPopup(auth, provider);
        const googleUser = result.user;

        const newUser = new User({
          firstName: googleUser.displayName.split(" ")[0],
          lastName: googleUser.displayName.split(" ")[1] || "",
          email: googleUser.email,
          gender: googleUser.gender,
          password: googleUser.password
        });

        // Enviar os dados ao backend
        const response = await createUser(newUser);
        // Exibir mensagem de sucesso
        this.success = "Account created successfully!";
        console.log("User created:", response);

        const router = useRouter();

        setTimeout(() => {
          console.log("Redirecionando para /login");
          router.push("/login");
        },2000);

      }catch (err) {
        // Exibir mensagem de erro
        this.error = err.response?.data?.message || "An error occurred during registration.";
        console.error(err);
      }
    },
  },
};
</script>

<style>
.section {
  padding-top: 50px;
}
.shape {
  position: absolute;
  width: 100%;
  height: 100%;
  z-index: 1;
}

.shape span {
  display: block;
  position: absolute;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.15);
}

.card {
  border-radius: 0.375rem;
}

.btn-neutral {
  background-color: #f6f9fc;
  border-color: #f6f9fc;
  color: #5e72e4;
  font-weight: 600;
}

.btn-neutral:hover {
  background-color: #dae3ec;
}

.input-group-alternative .form-control {
  border: 1px solid #cad1d7;
  padding: 0.75rem 1rem;
}

.form-check-label a {
  text-decoration: underline;
  color: #5e72e4;
}
</style>
