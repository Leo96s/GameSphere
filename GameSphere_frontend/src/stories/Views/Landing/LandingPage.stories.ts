import type { Meta, StoryObj } from '@storybook/vue3';
import LandingPage from '../../../views/Landing/LandingPage.vue';
import { vueRouter } from 'storybook-vue3-router';

const meta: Meta<typeof LandingPage> = {
  title: 'Views/LandingPage',
  component: LandingPage,
  decorators: [vueRouter()],
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;
type Story = StoryObj<typeof LandingPage>;

/**
 * Default view of the Landing Page.
 * Displays the Hero, Features, and CTA sections.
 */
export const Default: Story = {};

/**
 * Mobile view to test responsiveness of the feature grid.
 */
export const Mobile: Story = {
  parameters: {
    viewport: {
      defaultViewport: 'mobile1',
    },
  },
};