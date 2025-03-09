import { createUser } from "@/services/userServices";
import User from "@/models/User";
import { Gender } from "@/enums/Gender";
import {
  auth,
  githubProvider,
  googleProvider,
  signInWithPopup,
} from "@/services/firebase";

export default {
  async handleRegister() {
    if (this.isSubmitting) return;
    this.isSubmitting = true;

    try {
      this.error = null;
      this.success = null;
      console.log(
        this.firstName,
        this.lastName,
        this.gender,
        this.email,
        this.password
      );

      if (
        !this.firstName.trim() ||
        !this.lastName.trim() ||
        this.gender === "" ||
        !this.email.trim() ||
        !this.password.trim()
      ) {
        this.error = "All fields are required.";
        return;
      }

      const newUser = new User({
        firstName: this.firstName,
        lastName: this.lastName,
        email: this.email,
        gender: this.gender,
        password: this.password,
      });

      const response = await createUser(newUser);
      this.success = "Account created successfully!";
      console.log("User created:", response);

      setTimeout(() => {
        this.$router.push("/login");
      }, 2000);
    } catch (err) {
      this.error =
        err.response?.data?.message || "An error occurred during registration.";
      console.error(err);
    }
  },

  async registerWithGoogle() {
    try {
      const result = await signInWithPopup(auth, googleProvider);
      const googleUser = result.user;

      const newGoogleUser = new User({
        firstName: googleUser.displayName.split(" ")[0],
        lastName: googleUser.displayName.split(" ")[1] || "",
        email: googleUser.email,
        gender: Gender.Other,
        password: Math.random().toString(36).slice(-8),
      });

      newGoogleUser.uid = googleUser.uid;

      console.log(newGoogleUser);

      const response = await createUser(newGoogleUser);
      this.success = "Account created successfully!";
      console.log("User created:", response);

      setTimeout(() => {
        this.$router.push("/login");
      }, 2000);
    } catch (err) {
      this.error =
        err.response?.data?.message || "An error occurred during registration.";
      console.error(err);
    }
  },

  async registerWithGithub() {
    try {
      const result = await signInWithPopup(auth, githubProvider);
      const githubUser = result.user;

      const newGithubUser = new User({
        firstName: githubUser.displayName?.split(" ")[0] || "GitHub",
        lastName: githubUser.displayName?.split(" ")[1] || "User",
        email: githubUser.email,
        gender: Gender.Other,
        password: Math.random().toString(36).slice(-8),
      });

      newGithubUser.uid = githubUser.uid;

      console.log("Novo utilizador GitHub:", newGithubUser);

      const response = await createUser(newGithubUser);
      this.success = "Conta criada com sucesso!";
      console.log("Utilizador registrado:", response);

      setTimeout(() => {
        this.$router.push("/login");
      }, 2000);
    } catch (err) {
      this.error =
        err.response?.data?.message ||
        "Ocorreu um erro ao fazer login com GitHub.";
      console.error(err);
    }
  },
};
