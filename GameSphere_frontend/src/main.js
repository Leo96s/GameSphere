// Importando o Vue e os outros módulos necessários
import { createApp } from "vue"; // Importação correta para o Vue 3
import App from "./App.vue";
import router from "./router";


import '@fortawesome/fontawesome-free/css/all.css'
// Criando a aplicação Vue
const app = createApp(App);

app.use(router);
app.mount("#app");

