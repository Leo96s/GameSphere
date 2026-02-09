<template>
    <footer class="bg-gray-900 text-gray-300 mt-auto">
      <div class="max-w-7xl mx-auto px-6 py-12">

        <div class="grid md:grid-cols-2 gap-8 mb-10">
          <div>
            <h3 class="text-primary text-xl font-light mb-2">Thank you for supporting us!</h3>
            <h4 class="text-gray-400">Let's get in touch on any of these platforms.</h4>
          </div>

          <div class="flex md:justify-end gap-4 text-xl">
            <a href="#" class="hover:text-blue-400"><i class="fa fa-twitter"></i></a>
            <a href="#" class="hover:text-blue-600"><i class="fa fa-facebook-square"></i></a>
            <a href="#" class="hover:text-pink-500"><i class="fa fa-dribbble"></i></a>
            <a href="#" class="hover:text-gray-100"><i class="fa fa-github"></i></a>
          </div>
        </div>

        <hr class="border-gray-700 mb-6">

        <div class="flex flex-col md:flex-row justify-between items-center text-sm text-gray-400">

          <div class="flex gap-6 mt-4 md:mt-0">
            <a href="#" class="hover:text-white">About Us</a>
            <a href="#" class="hover:text-white">Blog</a>
            <a href="#" class="hover:text-white">License</a>
          </div>

          <div class="bg-opacity-25 bg-black text-center text-white">
            © {{ year }} Creative Tim & Binar Code
          </div>

        </div>

      </div>
    </footer>
  </template>

  <script>
  /**
   * AppFooter Component
   * * The global footer for the GameSphere application.
   * Features social media links, navigation shortcuts, and a dynamic copyright year.
   * * @component
   */
  export default {
    name: 'app-footer',
    /**
     * Component data
     * @returns {Object}
     * @property {number} year - The current full year for copyright display.
     */
    data() {
      return {
        year: new Date().getFullYear()
      }
    }
  };
  </script>
