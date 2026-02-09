import type { Meta, StoryObj } from '@storybook/vue3';
import AppFooter from '../../../layout/AppFooter.vue';

const meta: Meta<typeof AppFooter> = {
  title: 'Layout/Footer',
  component: AppFooter,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;
type Story = StoryObj<typeof AppFooter>;

/**
 * Standard view of the footer.
 * Note: FontAwesome icons must be loaded in the project for social icons to appear.
 */
export const Default: Story = {
  render: () => ({
    components: { AppFooter },
    template: `
      <div class="min-h-screen flex flex-col">
        <div class="flex-grow p-10 bg-gray-100">
          <p class="text-gray-500">Page Content Area</p>
        </div>
        <AppFooter />
      </div>
    `,
  }),
};