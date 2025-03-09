<template>
  <div class="profile-page">
    <!-- Profile Cover Section -->
    <section class="section-profile-cover section-shaped my-0">
      <div class="shape shape-style-1 shape-primary shape-skew alpha-4"></div>
    </section>

    <!-- Profile Card Section -->
    <section class="section section-skew">
      <div class="container">
        <div class="card shadow card-profile mt--300">
          <div class="px-4">
            <div class="row justify-content-center">
              <!-- Profile Image -->
              <div class="col-lg-3 order-lg-2">
                <div v-if="user" class="card-profile-image">
                  <a href="#">
                    <!-- <img :src="user.profileImage || "
                        alt="Profile Image"
                        class="rounded-circle"
                      />-->
                  </a>
                </div>
              </div>

              <!-- Profile Actions -->
              <div class="col-lg-4 order-lg-3 text-lg-right align-self-lg-center">
                <div class="card-profile-actions py-4 mt-lg-0">
                  <button class="btn btn-info btn-sm mr-4">Connect</button>
                  <button class="btn btn-default btn-sm">Message</button>
                </div>
              </div>

              <!-- Profile Stats -->
              <div class="col-lg-4 order-lg-1">
                <div class="card-profile-stats d-flex justify-content-center">
                  <div>
                    <span class="heading">22</span>
                    <span class="description">Friends</span>
                  </div>
                  <div>
                    <span class="heading">10</span>
                    <span class="description">Photos</span>
                  </div>
                  <div>
                    <span class="heading">89</span>
                    <span class="description">Comments</span>
                  </div>
                </div>
              </div>
            </div>

            <!-- Profile Info -->
            <div v-if="user" class="text-center mt-5">
              <h3>
                {{ user.firstName }} {{ user.lastName }}
                <span class="font-weight-light"> {{ timeSinceRegistration }}.</span>
              </h3>
              <div class="h6 font-weight-300">
                <i class="ni location_pin mr-2"></i>{{ user.location || "Location not provided" }}
              </div>
              <div class="h6 mt-4">
                <i class="ni business_briefcase-24 mr-2"></i>Solution Manager - Creative Tim Officer
              </div>
              <div>
                <i class="ni education_hat mr-2"></i>University of Computer Science
              </div>
            </div>

            <!-- Profile Bio -->
            <div class="mt-5 py-5 border-top text-center">
              <div class="row justify-content-center">
                <div class="col-lg-9">
                  <p>
                    An artist of considerable range, Ryan — the name taken by
                    Melbourne-raised, Brooklyn-based Nick Murphy — writes,
                    performs and records all of his own music, giving it a warm,
                    intimate feel with a solid groove structure. An artist of
                    considerable range.
                  </p>
                  <a href="#">Show more</a>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<script>
export default {
  name: "ProfilePage",
  data() {
    return {
      user: null
    };
  },
  mounted() {
    const storedUser = localStorage.getItem("user");
    console.log("Utilizador recuperado do localStorage:", storedUser);
    if (storedUser) {
      this.user = JSON.parse(storedUser);
    }
  },
  computed: {
    timeSinceRegistration() {
      if (!this.user || !this.user.registrationDate) return "Data inválida";

      const registrationDate = new Date(this.user.registrationDate);
      const currentDate = new Date();

      // Diferença em milissegundos
      const diffTime = currentDate - registrationDate;

      // Conversões de tempo
      const oneDay = 1000 * 60 * 60 * 24;
      const oneMonth = oneDay * 30; // Aproximação de um mês
      const oneYear = oneDay * 365; // Aproximação de um ano

      // Calcula a diferença
      const years = Math.floor(diffTime / oneYear);
      const months = Math.floor(diffTime / oneMonth);
      const days = Math.floor(diffTime / oneDay);

      // Retorna o tempo no formato correto
      if (years > 0) {
        return `pertence ao site há ${years} ${years === 1 ? "ano" : "anos"}`;
      } else if (months > 0) {
        return `pertence ao site há ${months} ${months === 1 ? "mês" : "meses"}`;
      } else {
        return `pertence ao site há ${days} ${days === 1 ? "dia" : "dias"}`;
      }
    }
  }
};
</script>

<style>
/* Estilo básico */
.profile-page .section-profile-cover {
  position: relative;
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

.card-profile {
  z-index: 2;
  position: relative;
}

.card-profile-image img {
  max-width: 140px;
  border: 3px solid #fff;
}

.card-profile-stats div {
  margin: 0 10px;
  text-align: center;
}

.card-profile-stats .heading {
  font-size: 1.2rem;
  font-weight: bold;
}

.card-profile-stats .description {
  font-size: 0.9rem;
  color: #6c757d;
}
</style>
