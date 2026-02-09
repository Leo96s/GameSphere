# 🚀 Landing Page

The `LandingPage` is the first touchpoint for unauthenticated users. It is designed to convert visitors into registered users through clear value propositions.

## 🧱 Sections

### 1. Hero Section
The "Above the Fold" area. It uses a high-contrast heading and a primary action button leading to the `/register` route.

### 2. Features Grid
A three-column grid (on desktop) highlighting the core pillars of GameSphere:
- **Quizzes:** Using `AcademicCapIcon`.
- **Cloud Integration:** Using `CloudIcon`.
- **Security:** Using `ShieldCheckIcon`.

### 3. Call to Action (CTA)
A high-visibility section with a purple background (`bg-purple-600`) to drive final conversions at the bottom of the page.

## 🛠 Technical Details

### Dependencies
- **Shadcn UI Button:** Used for consistent button styling.
- **Heroicons:** Used for the vector icons in the features section.
- **Vue Router:** Handles navigation to the registration page.

### Styling
The component uses Tailwind CSS for layout and a scoped style block for smooth hover transitions:
```css
.feature-box {
  transition: all 0.3s ease;
}