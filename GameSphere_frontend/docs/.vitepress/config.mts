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
    sidebar: [
      {
        text: '🚀 Introdução',
        items: [
          { text: 'Sobre o GameSphere', link: '/' },
          { text: 'Instalação', link: '/setup' },
        ]
      },
      {
        text: '🔐 Core (Lógica)',
        items: [
          { text: 'Autenticação', link: '/Auth/auth' },
          { text: 'Consumo de API', link: '/api' },
        ]
      },
      {
        text: '🎨 UI & Estilo',
        items: [
          { text: 'Cores e Tipografia', link: '/design' },
          { text: 'Guia de Componentes', link: '/componentes' },
        ]
      }
    ]
  }
})