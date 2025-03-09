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

                <CustomSelect label="Gender" v-model="gender" :options="[
                  { label: 'Male', value: Gender.Male },
                  { label: 'Female', value: Gender.Female },
                  { label: 'Other', value: Gender.Other }
                ]" />

                <CustomField label="Email" type="email" placeholder="Email" v-model="email" />
                <CustomField label="Password" type="password" placeholder="Password" v-model="password" />

                <div class="text-muted font-italic">
                  <small>
                    Password strength:
                    <span :class="passwordStrengthClass">
                      {{ passwordStrength }}
                    </span>
                  </small>
                </div>

                <CustomCheckbox id="privacyPolicy" v-model="acceptedPolicy">
                  I agree with the <a href="#">Privacy Policy</a>
                </CustomCheckbox>

                <CustomButton label="Create Account" type="submit" variant="primary"
                  :disabled="isSubmitting || !acceptedPolicy"/>
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
import CustomSelect from "@/views/components/CustomSelect.vue";
import CustomCheckbox from "@/views/components/CustomCheckbox.vue";
import CustomButton from "@/views/components/CustomButton.vue";

import registerMethods from "@/views/Register/methods";
import registerComputed from "@/views/Register/computed";

export default {
  name: "RegisterSection",
  components: { CustomField, CustomSelect, CustomCheckbox, CustomButton },
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
  computed: registerComputed,
  methods: registerMethods
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
