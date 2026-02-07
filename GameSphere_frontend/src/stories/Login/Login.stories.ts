import type { Meta, StoryObj } from '@storybook/vue3';
import Login from '../../views/Login/Login.vue';
import { userEvent, within, expect } from '@storybook/test';

// Este decorator simula o ambiente do Router para que o <RouterLink> funcione
import { vueRouter } from 'storybook-vue3-router';

const meta: Meta<typeof Login> = {
  title: 'Views/Login',
  component: Login,
  decorators: [
    vueRouter(), // Adiciona suporte ao RouterLink e useRouter()
  ],
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;
type Story = StoryObj<typeof Login>;

/* --- HISTÓRIA PADRÃO --- */
export const Default: Story = {};

/* --- TESTE DE INTERAÇÃO (Simula Erros de Validação) --- */
export const ValidationErrors: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const submitBtn = canvas.getByRole('button', { name: /Log In/i });

    // Clica no botão sem preencher nada para disparar o Zod
    await userEvent.click(submitBtn);

    // Verifica se as mensagens do Zod aparecem
    await expect(canvas.getByText(/Email is required/i)).toBeInTheDocument();
    await expect(canvas.getByText(/Password is required/i)).toBeInTheDocument();
  },
};

/* --- VISUALIZAÇÃO EM MOBILE --- */
export const Mobile: Story = {
  parameters: {
    viewport: {
      defaultViewport: 'mobile1',
    },
  },
};

/* --- ESTADO DE CARREGAMENTO (Simulado) --- */
// Nota: Para este funcionar perfeitamente, o componente precisaria
// de uma prop 'loading' ou usaríamos um Mock do Service.
export const LoadingState: Story = {
    // Exemplo de como forçar o estado interno se necessário
};