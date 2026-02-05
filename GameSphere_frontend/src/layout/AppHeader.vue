<template>
  <header class="bg-purple-600 text-white">
    <div class="container mx-auto flex items-center justify-between py-3 px-4">
      <!-- Logo / Brand -->
      <router-link to="/" class="text-xl font-bold">
        GameSphere
      </router-link>

      <!-- Menu Principal -->
      <nav class="hidden lg:flex items-center space-x-4">
        <router-link to="#" class="hover:underline">Quizzes</router-link>
        <span class="opacity-50 cursor-not-allowed">Disabled</span>
        <router-link v-if="!user" to="/login" class="hover:underline">Sign-in</router-link>
        <router-link v-if="!user" to="/register" class="hover:underline">Register</router-link>
      </nav>

      <!-- Right Side -->
      <div class="flex items-center space-x-4">
        <!-- Search -->
        <form @submit.prevent class="flex items-center space-x-2">
          <input
            type="text"
            placeholder="Search"
            class="px-2 py-1 rounded border border-gray-300 text-black bg-white"
          />
          <button
            type="submit"
            class="px-3 py-1 bg-white text-purple-600 rounded hover:bg-gray-100"
          >
            Search
          </button>
        </form>

        <!-- Language Dropdown -->
        <DropdownMenu>
          <DropdownMenuTrigger>
            Lang
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem>EN</DropdownMenuItem>
            <DropdownMenuItem>ES</DropdownMenuItem>
            <DropdownMenuItem>RU</DropdownMenuItem>
            <DropdownMenuItem>FA</DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>

        <!-- User Dropdown -->
        <DropdownMenu v-if="user">
          <DropdownMenuTrigger>
            <em>User</em>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem>
              <router-link to="/profile">Profile</router-link>
            </DropdownMenuItem>
            <DropdownMenuItem @click="handleLogout">Sign Out</DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      <!-- Mobile Toggle -->
      <button @click="isOpen = !isOpen" class="lg:hidden focus:outline-none">
        <svg
          class="w-6 h-6"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
          xmlns="http://www.w3.org/2000/svg"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M4 6h16M4 12h16M4 18h16"
          ></path>
        </svg>
      </button>
    </div>

    <!-- Mobile Menu -->
    <div v-if="isOpen" class="lg:hidden bg-purple-600 text-white px-4 py-2 space-y-2">
      <router-link to="#" class="block hover:underline">Quizzes</router-link>
      <span class="block opacity-50 cursor-not-allowed">Disabled</span>
      <router-link v-if="!user" to="/login" class="block hover:underline">Sign-in</router-link>
      <router-link v-if="!user" to="/register" class="block hover:underline">Register</router-link>
      <div class="border-t border-purple-500 pt-2">
        <!-- Language Dropdown for mobile -->
        <DropdownMenu>
          <DropdownMenuTrigger>Lang</DropdownMenuTrigger>
          <DropdownMenuContent align="start">
            <DropdownMenuItem>EN</DropdownMenuItem>
            <DropdownMenuItem>ES</DropdownMenuItem>
            <DropdownMenuItem>RU</DropdownMenuItem>
            <DropdownMenuItem>FA</DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>

        <!-- User Dropdown for mobile -->
        <DropdownMenu v-if="user">
          <DropdownMenuTrigger>User</DropdownMenuTrigger>
          <DropdownMenuContent align="start">
            <DropdownMenuItem>
              <router-link to="/profile">Profile</router-link>
            </DropdownMenuItem>
            <DropdownMenuItem @click="handleLogout">Sign Out</DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </div>
  </header>
</template>

<script setup>
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import { logout } from "@/services/authService";

// ChadCN UI Components
import {
  DropdownMenu,
  DropdownMenuTrigger,
  DropdownMenuContent,
  DropdownMenuItem,
} from "@/components/ui/dropdown-menu"; // ajuste o caminho conforme sua estrutura

const router = useRouter();
const user = ref(null);
const isOpen = ref(false);

onMounted(() => {
  const storedUser = localStorage.getItem("user");
  if (storedUser) user.value = JSON.parse(storedUser);
});

const handleLogout = async () => {
  await logout();
  user.value = null;
  router.push("/");
};
</script>

<style>
/* Tailwind já cobre grande parte do estilo, só ajustes se precisar */
</style>
