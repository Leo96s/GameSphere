import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import tailwindcss from '@tailwindcss/vite';
import path from 'path';
import fs from 'node:fs';
import { fileURLToPath } from 'node:url';
import { storybookTest } from '@storybook/addon-vitest/vitest-plugin';
import { playwright } from '@vitest/browser-playwright';

// Em ES Modules, o __dirname não existe por padrão. Criamos ele assim:
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const certFile = process.env.DEV_HTTPS_CERT_FILE;
const keyFile = process.env.DEV_HTTPS_KEY_FILE;
const useDevHttps = Boolean(certFile && keyFile && fs.existsSync(certFile) && fs.existsSync(keyFile));
if ((certFile || keyFile) && !useDevHttps) {
  console.warn(
    '[vite] DEV_HTTPS_CERT_FILE/DEV_HTTPS_KEY_FILE definidos mas os ficheiros não existem: o Vite corre em HTTP simples, sem proxies (/api, /__/auth). ' +
      'Gera os certificados com mkcert (ver README, secção do frontend de desenvolvimento em HTTPS): ' +
      'mkcert -install && mkcert -cert-file certs/localhost.pem -key-file certs/localhost-key.pem localhost 127.0.0.1 ::1',
  );
}
const firebaseAuthProxy = {
  target: 'https://gamesphere-9f7dc.firebaseapp.com',
  changeOrigin: true,
  secure: true,
};

// HTTPS e proxies só existem em dev, quando os certificados mkcert estão montados.
const devServer = useDevHttps
  ? {
      https: { cert: fs.readFileSync(certFile), key: fs.readFileSync(keyFile) },
      proxy: {
        '/api': {
          target: process.env.DEV_API_PROXY_TARGET || 'http://localhost:5095',
          changeOrigin: true,
        },
        '/__/auth': firebaseAuthProxy,
        '/__/firebase': firebaseAuthProxy,
      },
    }
  : undefined;

export default defineConfig({
  plugins: [
    vue(),
    tailwindcss()
  ],
  server: devServer,
  resolve: {
    alias: {
      // Garante que o '@' aponte corretamente para a pasta src
      '@': path.resolve(__dirname, './src'),
    },
  },
  test: {
    projects: [
      {
        extends: true,
        test: {
          name: 'unit',
          environment: 'node',
          include: ['src/**/*.spec.js'],
        },
      },
      {
        extends: true,
        plugins: [
          // Integração oficial do Storybook com Vitest
          storybookTest({
            configDir: path.join(__dirname, '.storybook'),
          }),
        ],
        test: {
          name: 'storybook',
          browser: {
            enabled: true,
            headless: true,
            provider: playwright({}),
            instances: [
              {
                browser: 'chromium',
              },
            ],
          },
          setupFiles: ['.storybook/vitest.setup.ts'],
        },
      },
    ],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json-summary', 'html'],
      include: [
        'src/router.js',
        'src/models/User.js',
        'src/services/*.js',
        'src/composables/useQuizAttempt.js',
        'src/utils/passwordStrength.ts',
        'src/utils/socialAuthFeedback.js',
      ],
      exclude: ['src/**/*.spec.js'],
      thresholds: {
        statements: 75,
        lines: 75,
        functions: 70,
        branches: 65,
      },
    },
  },
});
