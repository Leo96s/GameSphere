import { createRouter, createWebHistory } from 'vue-router'; // Alteração aqui
import AppHeader from "./layout/AppHeader";
import AppFooter from "./layout/AppFooter";
import Landing from "./views/LandingPage.vue";
import Login from "./views/Sign-InPage.vue";
import RegisterPage from "./views/RegisterPage.vue";
import Profile from "./views/ProfilePage.vue";

const routes = [
  {
    path: "/",
    name: "landing",
    components: {
      header: AppHeader,
      default: Landing,
      footer: AppFooter
    }
  },
  {
    path: "/login",
    name: "login",
    components: {
      header: AppHeader,
      default: Login,
      footer: AppFooter
    }
  },
  {
    path: "/register",
    name: "register",
    components: {
      header: AppHeader,
      default: RegisterPage,
      footer: AppFooter
    }
  },
  {
    path: "/profile",
    name: "profile",
    components: {
      header: AppHeader,
      default: Profile,
      footer: AppFooter
    }
  }
];

// Criando o roteador com createRouter e configurando o histórico
const router = createRouter({
  history: createWebHistory(), // Usando createWebHistory para navegação baseada em URL
  routes,
  linkExactActiveClass: "active",
  scrollBehavior: (to) => {
    if (to.hash) {
      return { selector: to.hash };
    } else {
      return { x: 0, y: 0 };
    }
  }
});

export default router;

