import { login, social_login } from "@/services/authService";
import { auth, githubProvider, googleProvider, signInWithPopup } from "@/services/firebase";


export default {
    async handleLogin() {
        try {
          this.error = null;
          this.success = null;
  
          if (!this.email || !this.password) {
            this.error = "All fields are required.";
            return;
          }
  
          const response = await login(this.email, this.password);
  
          if (response.data) {
            localStorage.setItem("user", JSON.stringify(response.data));
          } else {
            console.error("Erro: Nenhum dado recebido do login.");
          }
  
          this.success = "Login successfully!";
          console.log("User login-in:", response);
  
          setTimeout(() => {
            this.$router.push('/profile').then(() => {
              location.reload();
            });
          }, 500);
        } catch (err) {
          this.error = err.response?.data?.message || "An error occurred during registration.";
          console.error(err);
        }
      },
      async signinWithGoogle() {
        try {
          const result = await signInWithPopup(auth, googleProvider);
          const googleUser = result.user;
  
          const response = await social_login(googleUser.uid, googleUser.email);
  
          if (response) {
            localStorage.setItem("user", JSON.stringify(response.user));
            this.user = response.user;
          } else {
            console.error("Erro: Nenhum dado recebido do login.");
          }
  
          this.success = "Login successfully!";
          console.log("User sign-in:", response);
  
          setTimeout(() => {
            this.$router.push("/profile").then(() => {
              location.reload();
            });
          }, 500);
        } catch (err) {
          this.error = err.response?.data?.message || "An error occurred during registration.";
          console.error(err);
        }
      },
      async signinWithGithub() {
        try {
          const result = await signInWithPopup(auth, githubProvider);
          const githubUser = result.user;
  
          const response = await social_login(githubUser.uid, githubUser.email);
  
          if (response) {
            localStorage.setItem("user", JSON.stringify(response.user));
            this.user = response.user;
          } else {
            console.error("Erro: Nenhum dado recebido do login.");
          }
  
          this.success = "Login successfully!";
          setTimeout(() => {
            this.$router.push("/profile").then(() => {
              location.reload();
            });
          }, 500);
        } catch (err) {
          this.error = err.response?.data?.message || "An error occurred during registration.";
          console.error(err);
        }
      },
      
}