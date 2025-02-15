<template>
  <div id="app">
    <router-view name="header"></router-view>
    <main>
      <transition name="fade" mode="out-in">
        <router-view/>
      </transition>
    </main>
    <router-view name="footer"></router-view>
  </div>
</template>

<script>
export default {
  name: "App"
};
</script>

<style scoped>
.fade-enter-active, .fade-leave-active {
  transition: opacity 0.5s;
}
.fade-enter, .fade-leave-to {
  opacity: 0;
}
</style>


