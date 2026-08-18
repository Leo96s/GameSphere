<template>
  <div id="app" class="min-h-screen flex flex-col">

    <AppHeader />

    <main class="flex-grow">
        <router-view/>

    </main>

    <AppFooter />

  </div>
</template>

<script>
import {defineComponent} from "vue";
import AppHeader from "@/layout/AppHeader.vue";
import AppFooter from "@/layout/AppFooter.vue";

export default defineComponent({
  components: {AppFooter, AppHeader}
})
</script>

<style scoped>
.fade-enter-active, .fade-leave-active {
  transition: opacity 0.5s;
}
.fade-enter, .fade-leave-to {
  opacity: 0;
}
</style>


