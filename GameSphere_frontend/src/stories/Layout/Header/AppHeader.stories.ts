import type { Meta, StoryObj } from '@storybook/vue3';
import AppHeader from '../../../layout/AppHeader.vue'; // Adjust path if needed
import { vueRouter } from 'storybook-vue3-router';

const meta: Meta<typeof AppHeader> = {
  title: 'Layout/Header',
  component: AppHeader,
  decorators: [vueRouter()],
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;
type Story = StoryObj<typeof AppHeader>;

/**
 * Header state when no user is authenticated.
 */
export const LoggedOut: Story = {
  render: () => ({
    components: { AppHeader },
    setup() {
      localStorage.removeItem('user');
    },
    template: '<AppHeader />',
  }),
};

/**
 * Header state representing an authenticated user.
 */
export const LoggedIn: Story = {
  render: () => ({
    components: { AppHeader },
    setup() {
      const mockUser = {
        firstName: 'John',
        lastName: 'Doe',
        email: 'john@gamesphere.com'
      };
      localStorage.setItem('user', JSON.stringify(mockUser));
      // Manually trigger the event so the component picks it up in Storybook
      window.dispatchEvent(new Event('user-logged-in'));
    },
    template: '<AppHeader />',
  }),
};

/**
 * Mobile view simulation of the header.
 */
export const MobileView: Story = {
  parameters: {
    viewport: {
      defaultViewport: 'mobile1',
    },
  },
};