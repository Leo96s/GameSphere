<template>
    <div class="landing-page">
      <!-- Hero Section -->
      <section class="hero-section text-center py-5">
        <div class="container">
          <h1 class="display-4">Welcome to Our Service</h1>
          <p class="lead">You can have fun here!</p>
          <router-link to="/register" class="btn btn-light btn-lg">Get Started</router-link>
        </div>
      </section>

      <!-- Features Section -->
      <section class="features-section py-5">
        <div class="container text-center">
          <h2 class="mb-4">Our Features</h2>
          <div class="row">
            <div class="col-md-4">
              <div class="feature-box">
                <i class="fas fa-cogs fa-3x mb-3"></i>
                <h4>Quizzes</h4>
                <p>We provide tailored solutions that meet your specific needs.</p>
              </div>
            </div>
            <div class="col-md-4">
              <div class="feature-box">
                <i class="fas fa-cloud fa-3x mb-3"></i>
                <h4>Cloud Integration</h4>
                <p>Our services are seamlessly integrated with the cloud for scalability.</p>
              </div>
            </div>
            <div class="col-md-4">
              <div class="feature-box">
                <i class="fas fa-shield-alt fa-3x mb-3"></i>
                <h4>Security First</h4>
                <p>We ensure top-level security for your data and transactions.</p>
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- Call to Action Section -->
      <section class="cta-section bg-light text-center py-5">
        <div class="container">
          <h2>Ready to take a travel to a world of fun?</h2>
          <p class="lead">Join thousands of happy players.</p>
          <router-link to="/register" class="btn btn-primary btn-lg">Sign Up Now</router-link>
        </div>
      </section>

      <!-- Footer Section -->
      <footer class="footer bg-dark text-white text-center py-3">
        <p>&copy; 2025 Your Company. All rights reserved.</p>
      </footer>
    </div>
  </template>

  <script>
  export default {
    name: "LandingPage"
  };
  </script>

  <style scoped>
  .hero-section {
    background-color: lightgray;
    color: grey;
    padding: 80px 0;
  }

  .features-section {
    padding: 60px 0;
  }

  .feature-box {
    padding: 20px;
    border: 1px solid #ddd;
    border-radius: 8px;
    text-align: center;
    transition: all 0.3s ease;
  }

  .feature-box:hover {
    box-shadow: 0 4px 10px rgba(0, 0, 0, 0.1);
  }

  .cta-section {
    background-color: #f8f9fa;
    padding: 60px 0;
  }

  footer {
    background-color: #343a40;
  }

  footer p {
    margin: 0;
  }

  /* Button Styles */
  .btn-lg {
    padding: 12px 30px;
    font-size: 1.25rem;
    text-transform: uppercase;
  }

  @media (max-width: 767px) {
    .hero-section {
      padding: 50px 0;
    }
  }
  </style>

