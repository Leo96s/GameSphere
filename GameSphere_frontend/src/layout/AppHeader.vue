<template>
  <header class="bg-purple-600 text-white shadow-md relative z-50">
    <div class="w-full flex items-center justify-between py-3 px-10 h-16">

      <div class="flex items-center gap-8">
        <router-link to="/" class="text-xl font-bold tracking-wide hover:text-purple-100 transition-colors shrink-0">
          GameSphere
        </router-link>

        <nav class="hidden lg:flex items-center space-x-6">
          <router-link to="#" class="hover:text-purple-200 font-medium transition-colors text-sm">Quizzes</router-link>
          <span class="opacity-50 cursor-not-allowed font-medium text-sm">Disabled</span>

          <template v-if="!user">
            <router-link to="/login" class="hover:text-purple-200 font-medium transition-colors text-sm">Sign-in</router-link>
            <router-link to="/register" class="bg-white text-purple-600 px-4 py-1.5 rounded-full font-medium text-sm hover:bg-gray-100 transition-colors">
              Register
            </router-link>
          </template>
        </nav>
      </div>

      <div class="hidden md:flex flex-1 justify-center px-8">
        <form @submit.prevent class="w-full max-w-md relative">
          <input
            type="text"
            placeholder="Search for quizzes..."
            class="w-full bg-purple-700/50 text-white placeholder-purple-200 border border-purple-500 rounded-full py-1.5 pl-4 pr-10 focus:outline-none focus:bg-purple-700 focus:border-white transition-all text-sm"
          />
          <button type="submit" class="absolute right-3 top-1/2 -translate-y-1/2 text-purple-200 hover:text-white">
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/></svg>
          </button>
        </form>
      </div>

      <div class="flex items-center gap-3 shrink-0">

        <DropdownMenu>
          <DropdownMenuTrigger class="flex items-center gap-1 hover:bg-purple-700 px-2 py-1.5 rounded-md transition-colors outline-none text-sm font-medium">
            <span>EN</span>
            <svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="opacity-70"><path d="m6 9 6 6 6-6"/></svg>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem class="justify-center cursor-pointer">EN</DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem class="justify-center cursor-pointer">PT</DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>

        <DropdownMenu v-if="user">
          <DropdownMenuTrigger class="flex items-center gap-2 hover:bg-purple-700 pl-1 pr-2 py-1 rounded-full transition-colors outline-none border border-transparent hover:border-purple-500/30">
            <div class="w-8 h-8 bg-purple-200 text-purple-800 rounded-full flex items-center justify-center font-bold text-xs shadow-sm ring-2 ring-purple-500">
              {{ user.firstName?.charAt(0) }}{{ user.lastName?.charAt(0) }}
            </div>
            <span class="font-medium text-sm hidden xl:block max-w-[100px] truncate">
              {{ user.firstName }}
            </span>
          </DropdownMenuTrigger>

          <DropdownMenuContent align="end" class="w-48 mt-1">
            <div class="px-2 py-1.5 text-xs text-gray-500 font-semibold uppercase">
              My Account
            </div>
            <DropdownMenuItem class="cursor-pointer" @click="router.push('/profile')">
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="mr-2"><path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>
              Profile
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem @click="handleLogout" class="text-red-600 focus:text-red-600 focus:bg-red-50 cursor-pointer">
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="mr-2"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" x2="9" y1="12" y2="12"/></svg>
              Sign Out
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>

        <button @click="isOpen = !isOpen" class="lg:hidden p-2 rounded-md hover:bg-purple-700 focus:outline-none">
           <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path></svg>
        </button>
      </div>

    </div>

    <transition
      enter-active-class="transition duration-200 ease-out"
      enter-from-class="transform -translate-y-2 opacity-0"
      enter-to-class="transform translate-y-0 opacity-100"
      leave-active-class="transition duration-150 ease-in"
      leave-from-class="transform translate-y-0 opacity-100"
      leave-to-class="transform -translate-y-2 opacity-0"
    >
      <div v-if="isOpen" class="lg:hidden bg-purple-800 text-white px-4 py-4 shadow-inner border-t border-purple-700">
        <form @submit.prevent class="mb-4">
          <input type="text" placeholder="Search..." class="w-full bg-purple-900/50 border border-purple-600 rounded-md px-3 py-2 text-sm text-white placeholder-purple-400 focus:outline-none focus:border-purple-400">
        </form>

        <nav class="flex flex-col space-y-2">
          <router-link to="#" class="block py-2 hover:text-purple-300">Quizzes</router-link>

          <template v-if="!user">
             <router-link to="/login" class="block py-2 hover:text-purple-300">Sign-in</router-link>
             <router-link to="/register" class="block py-2 hover:text-purple-300">Register</router-link>
          </template>

          <template v-if="user">
             <div class="border-t border-purple-600 my-2 pt-2">
               <div class="text-xs uppercase text-purple-400 font-semibold mb-2">Account</div>
               <router-link to="/profile" class="flex items-center py-2 hover:text-purple-300">
                 <span class="w-6 h-6 bg-purple-500 rounded-full flex items-center justify-center mr-2 text-xs text-white">
                    {{ user.firstName?.charAt(0) }}
                 </span>
                 Profile
               </router-link>
               <button @click="handleLogout" class="w-full text-left py-2 text-red-300 hover:text-red-100 flex items-center">
                 Sign Out
               </button>
             </div>
          </template>
        </nav>
      </div>
    </transition>
  </header>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from "vue";
import { useRouter } from "vue-router";
import { logout } from "@/services/authService";

// ChadCN UI Components
import {
  DropdownMenu,
  DropdownMenuTrigger,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator
} from "@/components/ui/dropdown-menu";

const router = useRouter();
const user = ref(null);
const isOpen = ref(false);

const checkUser = () => {
  try {
    const storedUser = localStorage.getItem("user");
    // Se existir e não for a string "undefined" ou "null"
    if (storedUser && storedUser !== "undefined" && storedUser !== "null") {
      user.value = JSON.parse(storedUser);
    } else {
      user.value = null;
    }
  } catch (e) {
    console.error("Erro ao processar dados do utilizador:", e);
    user.value = null;
  }
};

const handleStorageUpdate = () => checkUser();

onMounted(() => {
  checkUser();

  // Ouve o evento de login manual que criámos
  window.addEventListener('user-logged-in', handleStorageUpdate);
  // Ouve mudanças feitas noutras janelas/abas
  window.addEventListener('storage', handleStorageUpdate);
});

// ⚠️ ESSENCIAL PARA EVITAR O ERRO DE "FLAGS" E "JOB" NO HMR
onUnmounted(() => {
  window.removeEventListener('user-logged-in', handleStorageUpdate);
  window.removeEventListener('storage', handleStorageUpdate);
});

const handleLogout = () => {
  logout();
  user.value = null;
  isOpen.value = false;
  router.push("/login");
};
</script>