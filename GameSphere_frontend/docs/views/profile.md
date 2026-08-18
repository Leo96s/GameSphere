# 👤 Profile Page

The `ProfilePage` provides a comprehensive view of the user's account details and their history within the GameSphere platform.

## 🎨 Visual Layout

The page is split into two main visual layers:

1. **The Cover (`section-profile-cover`):** A shaped, skewed background that defines the top brand area.
2. **The Profile Card:** A floating card with a negative top margin (`mt--300`) that creates an overlapping effect over the cover.

## 🛠 Features

### Dynamic Tenure Calculation

The component automatically calculates how long a user has been a member.

* **Years:** Calculated if duration > 365 days.
* **Months:** Calculated if duration > 30 days.
* **Days:** Default fallback for new users.

### Persistence Logic

The component is self-sufficient. On the `mounted` lifecycle hook, it checks for a `user` key in `localStorage`. This ensures that if the user navigates directly to `/profile`, their data is immediately available without additional API latency.

## 📊 Data Mapping

The view expects the following object structure from `localStorage`:

| Key | Type | Description |
| --- | --- | --- |
| `firstName` | String | User's first name. |
| `lastName` | String | User's last name. |
| `location` | String | (Optional) User's city/country. |
| `registrationDate` | Date String | ISO format date of account creation. |

## 🧩 UI Components Used

* **Argon Shape System:** Creates the skewed background effect.
* **Bootstrap Grid:** Manages the responsive stats and actions row.
* **Nucleo Icons:** Uses classes like `ni-location_pin` for semantic metadata display.

---

### Quick Tip for Layout

The overlapping effect is controlled by this specific CSS class:

```css
.card-profile {
  margin-top: -300px; /* Pulls the card up into the cover section */
}

```
