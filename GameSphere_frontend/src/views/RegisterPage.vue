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
              <div class="btn-wrapper text-center space-x-4">
                <div class="text-center my-3">
                  <b-button @click="registerWithGoogle" title="Google" class="btn-icon mx-2" variant="light">
                    <img src="@/assets/logos/google.png" alt="Google" class="icon-img" />
                  </b-button>

                  <b-button @click="registerWithGithub" class="btn-icon" variant="dark" title="Github">
                    <img src="@/assets/logos/github.png" alt="Github" class="icon-img" />
                  </b-button>
                </div>
              </div>
            </div>

            <!-- Card Body -->
            <div class="card-body px-lg-5">
              <div class="text-center text-muted mb-4">
                <small>Or register with your credentials</small>
              </div>
              <form @submit.prevent="handleRegister">
                <CustomField label="First Name" type="text" placeholder="First Name" v-model="firstName" />
                <CustomField label="Last Name" type="text" placeholder="Last Name" v-model="lastName" />
                
                <div class="form-group mb-3">
                  <label>Gender</label>
                  <select class="form-control" v-model="gender">
                    <option value="" disabled selected>Gender</option>
                    <option :value="Gender.Male">Male</option>
                    <option :value="Gender.Female">Female</option>
                    <option :value="Gender.Other">Other</option>
                  </select>
                </div>
                
                <CustomField label="Email" type="email" placeholder="Email" v-model="email" />
                <CustomField label="Password" type="password" placeholder="Password" v-model="password" />

                <div class="text-muted font-italic">
                  <small>
                    password strength: <span class="text-success font-weight-700">strong</span>
                  </small>
                </div>

                <div class="form-check mt-3">
                  <input type="checkbox" class="form-check-input" id="privacyPolicy" v-model="acceptedPolicy" />
                  <label class="form-check-label" for="privacyPolicy">
                    I agree with the <a href="#">Privacy Policy</a>
                  </label>
                </div>

                <div class="text-center">
                  <button type="submit" class="btn btn-primary my-4" :disabled="!acceptedPolicy">
                    Create account
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
        </div>
      </div>
    </div>
  </section>
</template>

<script>
import CustomField from "@/views/components/CustomField.vue";
import { createUser } from "@/services/userServices";
import User from "@/models/User";
import { Gender } from "@/enums/Gender";
import { auth, githubProvider, googleProvider, signInWithPopup } from "@/services/firebase";

export default {
  name: "RegisterSection",
  components: { CustomField },
  computed: {
    Gender() {
      return Gender;
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
        console.log(this.firstName, this.lastName, this.gender, this.email, this.password);

        // Valida os campos obrigatórios
        if (!this.firstName || !this.lastName || !this.gender || !this.email || !this.password) {
          this.error = "All fields are required.";
          return;
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

        setTimeout(() => {
          console.log("Redirecionando para /login");
          this.$router.push("/login");
        }, 2000);
      } catch (err) {
        // Exibir mensagem de erro
        this.error = err.response?.data?.message || "An error occurred during registration.";
        console.error(err);
      }
    },

    async registerWithGoogle() {
      try {
        const result = await signInWithPopup(auth, googleProvider);
        const googleUser = result.user;

        const newGoogleUser = new User({
          firstName: googleUser.displayName.split(" ")[0],
          lastName: googleUser.displayName.split(" ")[1] || "",
          email: googleUser.email,
          gender: Gender.Other,
          password:  Math.random().toString(36).slice(-8),
        });

        newGoogleUser.uid = googleUser.uid;

        console.log(newGoogleUser);

        // Enviar os dados ao backend
        const response = await createUser(newGoogleUser);
        // Exibir mensagem de sucesso
        this.success = "Account created successfully!";
        console.log("User created:", response);

        setTimeout(() => {
          console.log("Redirecionando para /login");
          this.$router.push("/login");
        }, 2000);
      } catch (err) {
        // Exibir mensagem de erro
        this.error = err.response?.data?.message || "An error occurred during registration.";
        console.error(err);
      }
    },
    async registerWithGithub() {
      try {
        const result = await signInWithPopup(auth, githubProvider);
        const githubUser = result.user;

        // Criar um usuário com os dados do GitHub
        const newGithubUser = new User({
          firstName: githubUser.displayName?.split(" ")[0] || "GitHub",
          lastName: githubUser.displayName?.split(" ")[1] || "User",
          email: githubUser.email,
          gender: Gender.Other,
          password: Math.random().toString(36).slice(-8) // Senha aleatória
        });

        newGithubUser.uid = githubUser.uid;

        console.log("Novo utilizador GitHub:", newGithubUser);

        // Enviar os dados ao backend (caso tenha API para cadastro)
        const response = await createUser(newGithubUser);

        // Exibir mensagem de sucesso
        this.success = "Conta criada com sucesso!";
        console.log("Utilizador registrado:", response);

        setTimeout(() => {
          this.$router.push("/login");
        }, 2000);
      } catch (err) {
        this.error = err.response?.data?.message || "Ocorreu um erro ao fazer login com GitHub.";
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
