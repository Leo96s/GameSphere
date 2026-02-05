<template>
  <div class="landing-page">

    <!-- Hero Section -->
    <section class="hero-section text-center py-24 bg-gray-100">
      <div class="container mx-auto px-4">
        <h1 class="text-5xl font-bold mb-4 text-gray-800">Welcome to Our Service</h1>
        <p class="text-xl text-gray-600 mb-8">You can have fun here!</p>
        <Button asChild>
          <router-link to="/register" class="px-8 py-3 text-lg">
            Get Started
          </router-link>
        </Button>
      </div>
    </section>

    <!-- Features Section -->
    <section class="features-section py-24">
      <div class="container mx-auto px-4 text-center">
        <h2 class="text-4xl font-semibold mb-12">Our Features</h2>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-8">

          <!-- Feature 1 -->
          <div class="feature-box p-6 border rounded-lg hover:shadow-lg transition">
            <AcademicCapIcon class="w-16 h-16 text-purple-600 mx-auto mb-4"/>
            <h4 class="text-xl font-bold mb-2">Quizzes</h4>
            <p class="text-gray-600">We provide tailored solutions that meet your specific needs.</p>
          </div>

          <!-- Feature 2 -->
          <div class="feature-box p-6 border rounded-lg hover:shadow-lg transition">
            <CloudIcon class="w-16 h-16 text-purple-600 mx-auto mb-4"/>
            <h4 class="text-xl font-bold mb-2">Cloud Integration</h4>
            <p class="text-gray-600">Our services are seamlessly integrated with the cloud for scalability.</p>
          </div>

          <!-- Feature 3 -->
          <div class="feature-box p-6 border rounded-lg hover:shadow-lg transition">
            <ShieldCheckIcon class="w-16 h-16 text-purple-600 mx-auto mb-4"/>
            <h4 class="text-xl font-bold mb-2">Security First</h4>
            <p class="text-gray-600">We ensure top-level security for your data and transactions.</p>
          </div>

        </div>
      </div>
    </section>

    <!-- Call to Action Section -->
    <section class="cta-section bg-purple-600 text-white py-24 text-center">
      <div class="container mx-auto px-4">
        <h2 class="text-4xl font-bold mb-4">Ready to take a travel to a world of fun?</h2>
        <p class="text-xl mb-8">Join thousands of happy players.</p>
        <Button asChild variant="default">
          <router-link to="/register" class="px-8 py-3 text-lg">
            Sign Up Now
          </router-link>
        </Button>
      </div>
    </section>

  </div>
</template>

<script setup>
import { Button } from "@/components/ui/button";
// Heroicons (Tailwind official icons)
import { AcademicCapIcon, CloudIcon, ShieldCheckIcon } from '@heroicons/vue/24/outline';
</script>

<style scoped>
.feature-box {
  transition: all 0.3s ease;
}
</style>
