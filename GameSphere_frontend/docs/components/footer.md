# 👣 App Footer

The `Footer` component provides the final touchpoint for the user at the bottom of every page, containing social links and legal information.

## 🚀 Features

- **Dynamic Year:** Automatically updates the copyright year using the JavaScript `Date` object.
- **Social Integration:** Pre-configured slots for Twitter, Facebook, Dribbble, and GitHub.
- **Responsive Grid:** Switches from a 2-column layout on desktop to a stacked layout on mobile devices.
- **Sticky Friendly:** Uses `mt-auto` to push itself to the bottom of flex containers.

## ⚙️ Technical Logic

### Social Icons
The component uses **FontAwesome** classes (e.g., `fa fa-twitter`). Ensure the FontAwesome library is imported in your `main.ts` or global CSS for these to render correctly.

### Copyright Display
The footer displays a hardcoded attribution to *Creative Tim & Binar Code* as per the base template license:
```html
© {{ year }} Creative Tim & Binar Code

```

## 🎨 Layout Breakdown

| Section | Content | Styling |
| --- | --- | --- |
| **Call to Action** | "Thank you for supporting us!" | Primary color heading |
| **Social Links** | Platform icons | Flex-end alignment (md+) |
| **Nav Links** | About, Blog, License | Text-sm muted gray |
| **Copyright** | Year and authors | Black opacity background |

## 🔍 Storybook

View the footer implementation in different viewports here: [Footer Stories](https://www.google.com/search?q=http://localhost:6006/%3Fpath%3D/story/layout-footer--default).
