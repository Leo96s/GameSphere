// Importando o Vue e os outros módulos necessários
import { createApp } from "vue"; // Importação correta para o Vue 3
import App from "./App.vue";
import router from "./router";
import BootstrapVue3 from "bootstrap-vue-3";

import "bootstrap/dist/css/bootstrap.css";
import "bootstrap-vue-3/dist/bootstrap-vue-3.css";
// Criando a aplicação Vue
const app = createApp(App);

// Usando o roteador e montando o app no DOM
app.use(BootstrapVue3);
app.use(router);
app.mount("#app");

