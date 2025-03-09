import { Gender } from "@/enums/Gender";

export default {
  Gender() {
    return Gender;
  },
  passwordStrength() {
    if (this.password.length < 6) {
      return "Weak";
    } else if (this.password.length < 10) {
      return /\d/.test(this.password) && /[a-zA-Z]/.test(this.password) ? "Medium" : "Weak";
    } else {
      return /\d/.test(this.password) && /[a-zA-Z]/.test(this.password) && /[\W_]/.test(this.password)
        ? "Strong"
        : "Medium";
    }
  },
  passwordStrengthClass() {
    return {
      "text-danger": this.passwordStrength === "Weak",
      "text-warning": this.passwordStrength === "Medium",
      "text-success": this.passwordStrength === "Strong"
    };
  }
};
