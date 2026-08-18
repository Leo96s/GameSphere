# 🗺️ App Header

The `Header` component is the primary navigation bar for the GameSphere application. It handles user authentication states, global search, and language selection.

## 🚀 Features

- **Auth Sync:** Automatically updates when a user logs in or out using custom events.
- **Responsive Design:** Includes a mobile hamburger menu and a collapsed search bar.
- **Language Switcher:** Dropdown for switching between English (EN) and Portuguese (PT).
- **User Menu:** Custom dropdown for authenticated users showing their initials and profile links.

## ⚙️ Technical Logic

### Authentication Synchronization
The component doesn't rely on a global store like Pinia for its core user state. Instead, it listens to:
1. `user-logged-in`: A custom event dispatched by the `authService`.
2. `storage`: Standard browser event for cross-tab synchronization.

### User Initials
The avatar displays initials dynamically:
```javascript
{{ user.firstName?.charAt(0) }}{{ user.lastName?.charAt(0) }}

```

## 🎨 Visual States

| State | Elements Visible |
| --- | --- |
| **Guest** | Sign-in and Register buttons. |
| **User** | Search bar (md+), Language dropdown, User Avatar. |
| **Mobile** | Hamburger menu icon, Hidden desktop links. |

## 🔍 Storybook

Test the responsive behavior and auth transitions in our [Header Stories](https://www.google.com/search?q=http://localhost:6006/%3Fpath%3D/story/layout-header--logged-out).

