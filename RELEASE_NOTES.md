## v0.12.2 - 2026-09-07
* fix(auth): repair social login and add account linking, password toggle
* fix(security): require an explicit AllowedHosts outside Development (GAM-52)
* feat(account): replace deletion with deactivation, recovery, and anonymization (GAM-14)
* feat(account): add secure email and password change flows
* fix(docker): install missing GSSAPI library in production image
* docs: plan to reduce cyclomatic_complexity
* refactor(complexity): split high-complexity flows
* ci(testing): enforce coverage and Docker E2E
* test(frontend): add coverage and E2E flows
* test(backend): expand security and quiz coverage
* fix(security): harden auth and account flows
* ci: sync versioning workflows to 2.9.0
