import type { Meta, StoryObj } from '@storybook/vue3';
import Register from '../../../views/Register/RegisterPage.vue';
import { vueRouter } from 'storybook-vue3-router';
import { userEvent, within, expect } from '@storybook/test';

const meta: Meta<typeof Register> = {
  title: 'Views/Auth/Register',
  component: Register,
  decorators: [vueRouter()],
  parameters: { layout: 'fullscreen' },
};

export default meta;
type Story = StoryObj<typeof Register>;

export const Default: Story = {};

/**
 * Tests the "Required" field validation across the entire form.
 */
export const ValidationTrigger: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const createBtn = canvas.getByRole('button', { name: /Create Account/i });

    await userEvent.click(createBtn);

    // Check for specific Zod error messages
    await expect(canvas.getByText(/First name is required/i)).toBeInTheDocument();
    await expect(canvas.getByText(/You must accept the terms/i)).toBeInTheDocument();
  },
};

/**
 * Demonstrates the password strength meter UI.
 */
export const PasswordStrengthTest: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const pwdInput = canvas.getByLabelText(/Password/i);

    // Type a weak password
    await userEvent.type(pwdInput, '123456');
    // The meter should appear (visual check required or CSS class check)
  },
};