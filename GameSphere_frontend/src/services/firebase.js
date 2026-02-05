// Import the functions you need from the SDKs you need
import { initializeApp } from "firebase/app";
import { getAuth, GoogleAuthProvider, GithubAuthProvider, signInWithPopup, signInWithRedirect, signOut, getRedirectResult } from "firebase/auth";
// TODO: Add SDKs for Firebase products that you want to use
// https://firebase.google.com/docs/web/setup#available-libraries

// Your web app's Firebase configuration
// For Firebase JS SDK v7.20.0 and later, measurementId is optional
const firebaseConfig = {
  apiKey: "AIzaSyAvH8Q1_Hdn1WvYtd3UqO-DAwsG_LBMBV8",
  authDomain: "gamesphere-9f7dc.firebaseapp.com",
  projectId: "gamesphere-9f7dc",
  storageBucket: "gamesphere-9f7dc.firebasestorage.app",
  messagingSenderId: "608557567508",
  appId: "1:608557567508:web:590ac782faf1aa992a6101",
  measurementId: "G-TBCBJLJ2RY"
};

// Initialize Firebase
const app = initializeApp(firebaseConfig);
const auth = getAuth(app);

//providers
const googleProvider = new GoogleAuthProvider();
const githubProvider = new GithubAuthProvider();

export { auth, googleProvider, githubProvider, signInWithPopup, signInWithRedirect, signOut, getRedirectResult };