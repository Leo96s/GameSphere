import { defineConfig } from 'vitepress'
import { fileURLToPath, URL } from 'node:url' // Certifica-te que tem o prefixo 'node:'

export default defineConfig({
  title: "GameSphere",
  description: "Documentação do Frontend",
  vite: {
    configFile: false,
    resolve: {
      alias: [
        {
          find: '@',
          replacement: fileURLToPath(new URL('../../src', import.meta.url))
        }
      ]
    }
  },
  themeConfig: {
    // 1. Menu Superior (Nav)
    nav: [
      { text: 'Home', link: '/' },
      { text: 'Views', link: '/views/landing-page' },
      { text: 'Componentes', link: '/components/header' }
    ],

    // 2. Barra Lateral (Sidebar)
    // Aqui organizamos exatamente como as suas pastas estão no VS Code
    sidebar: [
      {
        text: '🚀 Views (Páginas)',
        items: [
          { text: 'Landing Page', link: '/views/landing-page' },
          { text: 'Login', link: '/views/login' },
          { text: 'Registro', link: '/views/register' },
          { text: 'Perfil', link: '/views/profile' },
          { text: 'Recuperar Senha', link: '/views/sent-code' },
          { text: 'Resetar Senha', link: '/views/reset-password' },
        ]
      },
      {
        text: '🧩 Componentes UI',
        items: [
          { text: 'Header', link: '/components/header' },
          { text: 'Footer', link: '/components/footer' },
          { text: 'Toast', link: '/components/toast' },
        ]
      },
      {
        text: '🔐 Autenticação',
        items: [
          { text: 'Fluxo de Auth', link: '/auth/auth' },
        ]
      }
    ],

    socialLinks: [
      { icon: 'github', link: 'https://github.com/Leo96s' }
    ]
  }
})