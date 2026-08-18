import type { Meta, StoryObj } from '@storybook/vue3';
import ResetPassword from '../../../views/Login/ResetPassword.vue';
import { vueRouter } from 'storybook-vue3-router';

const meta: Meta<typeof ResetPassword> = {
  title: 'Views/Auth/ResetPassword',
  component: ResetPassword,
  decorators: [vueRouter()],
  parameters: { layout: 'fullscreen' },
};

export default meta;
type Story = StoryObj<typeof ResetPassword>;

export const Default: Story = {
  render: () => ({
    components: { ResetPassword },
    setup() {
      // O seu código faz JSON.parse(localStorage.getItem("email"))
      // Por isso, precisamos de "double quotes" dentro da string
      localStorage.setItem("email", JSON.stringify("user@example.com"));
    },
    template: '<ResetPassword />'
  })
};