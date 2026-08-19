import { createRouter, createWebHistory } from 'vue-router';
import AppFooter from '@/layout/AppFooter.vue';
import AppHeader from '@/layout/AppHeader.vue';
import Landing from '@/views/Landing/LandingPage.vue';
import Login from '@/views/Login/Login.vue';
import RegisterPage from '@/views/Register/RegisterPage.vue';
import Profile from '@/views/Profile/ProfilePage.vue';
import SentCodePage from '@/views/Login/SentCodePage.vue';
import ResetPassword from '@/views/Login/ResetPassword.vue';
import QuizCatalogPage from '@/views/Quizzes/QuizCatalogPage.vue';
import QuizPlayPage from '@/views/Quizzes/QuizPlayPage.vue';
import QuizResultPage from '@/views/Quizzes/QuizResultPage.vue';
import AdminQuizListPage from '@/views/Admin/AdminQuizListPage.vue';
import AdminQuizEditorPage from '@/views/Admin/AdminQuizEditorPage.vue';
import { getCurrentRole } from '@/services/authService';

const getToken = () => localStorage.getItem('token');

export const createNavigationGuard = ({
  getToken: readToken = getToken,
  getCurrentRole: readCurrentRole = getCurrentRole,
} = {}) => (to) => {
  if (!to.meta.requiresAuth && !to.meta.requiresAdmin) {
    return true;
  }

  if (!readToken()) {
    return { name: 'landing' };
  }

  if (to.meta.requiresAdmin && readCurrentRole() !== 'Admin') {
    return { name: 'landing' };
  }

  return true;
};

export const routes = [
  {
    path: '/',
    name: 'landing',
    components: {
      header: AppHeader,
      default: Landing,
      footer: AppFooter,
    },
  },
  {
    path: '/login',
    name: 'login',
    components: {
      header: AppHeader,
      default: Login,
      footer: AppFooter,
    },
  },
  {
    path: '/register',
    name: 'register',
    components: {
      header: AppHeader,
      default: RegisterPage,
      footer: AppFooter,
    },
  },
  {
    path: '/profile',
    name: 'profile',
    components: {
      header: AppHeader,
      default: Profile,
      footer: AppFooter,
    },
    meta: { requiresAuth: true },
  },
  {
    path: '/quizzes',
    name: 'quiz-catalog',
    component: QuizCatalogPage,
    meta: { requiresAuth: true },
  },
  {
    path: '/quizzes/:id',
    name: 'quiz-play',
    component: QuizPlayPage,
    meta: { requiresAuth: true },
  },
  {
    path: '/quizzes/:id/result',
    name: 'quiz-result',
    component: QuizResultPage,
    meta: { requiresAuth: true },
  },
  {
    path: '/admin/quizzes',
    name: 'admin-quiz-list',
    component: AdminQuizListPage,
    meta: { requiresAuth: true, requiresAdmin: true },
  },
  {
    path: '/admin/quizzes/:id',
    name: 'admin-quiz-editor',
    component: AdminQuizEditorPage,
    meta: { requiresAuth: true, requiresAdmin: true },
  },
  {
    path: '/forgetPassword/sentCode',
    name: 'sentCode',
    components: {
      header: AppHeader,
      default: SentCodePage,
      footer: AppFooter,
    },
  },
  {
    path: '/forgetPassword/resetPassword',
    name: 'resetPassword',
    components: {
      header: AppHeader,
      default: ResetPassword,
      footer: AppFooter,
    },
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
  linkExactActiveClass: 'active',
  scrollBehavior: (to) => {
    if (to.hash) {
      return { selector: to.hash };
    }

    return { x: 0, y: 0 };
  },
});

router.beforeEach(createNavigationGuard());

export default router;
