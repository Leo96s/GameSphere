## v0.18.6 - 2026-10-25
* fix(deps): upgrade zod to v4 and patch Storybook and Vitest advisories
* fix(nginx): proxy the Firebase auth handler through a fixed upstream
* docs(readme): document HTTPS dev setup and Firebase social login
* fix(auth): sign out of Firebase on logout and show Firebase error codes
* fix(auth): serve the Firebase auth handler from the app origin
* docs(quiz): document the quiz preview flow (GAM-47)
* feat(admin): add quiz preview page with local grading and status actions (GAM-47)
* feat(quiz): add admin quiz preview and clear player not-found errors (GAM-47)
* docs(quiz): document draft authoring rules and public contract (GAM-46, GAM-48)
* feat(admin): translate editor errors and validate drafts client-side (GAM-46)
* feat(quiz): harden draft authoring and guard the public question contract (GAM-46, GAM-48)
