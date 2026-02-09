---
# https://vitepress.dev/reference/default-theme-home-page
layout: home

hero:
  name: "GameSphere Docs"
  text: "Frontend Documentation"
  tagline: Guia técnico de componentes, fluxos de autenticação e views.
  actions:
    - theme: brand
      text: Começar pelas Views
      link: /views/landing-page
    - theme: alt
      text: Biblioteca de Componentes
      link: /components/header
    - theme: alt
      text: Fluxo de Auth
      link: /auth/auth

features:
  - title: 🖼️ Views Principais
    details: Documentação da Landing Page, Perfil e fluxos de navegação do usuário.
    link: /views/landing-page
  - title: 🧩 Componentes UI
    details: Header, Footer, Toasts e elementos reutilizáveis com Shadcn/Tailwind.
    link: /components/header
  - title: 🔐 Segurança & Auth
    details: Detalhes sobre a integração com Firebase, login social e recuperação de senha.
    link: /auth/auth
  - title: 🧪 Storybook Integrado
    details: Como testar componentes isoladamente e garantir a consistência visual.
    link: /views/login
---

## 🛠 Estrutura do Projeto

Navegue rapidamente pelas seções detalhadas:

### Páginas de Autenticação (Views)
- [Página de Login](/views/login)
- [Registro de Usuário](/views/register)
- [Esqueci a Senha](/views/sent-code)
- [Resetar Senha](/views/reset-password)

### Componentes Globais
- [Header Dinâmico](/components/header)
- [Footer Informativo](/components/footer)
- [Sistema de Toast](/components/toast)

### Perfil & Landing
- [Dashboard de Perfil](/views/profile)
- [Landing Page](/views/landing-page)