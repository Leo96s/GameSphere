import type { Meta, StoryObj } from '@storybook/vue3';
import ProfilePage from '../../../views/Profile/ProfilePage.vue';

const meta: Meta<typeof ProfilePage> = {
  title: 'Views/ProfilePage',
  component: ProfilePage,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;
type Story = StoryObj<typeof ProfilePage>;

export const Default: Story = {
  render: () => ({
    components: { ProfilePage },
    setup() {
      // Mocking the user in localStorage for the story preview
      const mockUser = {
        firstName: 'Ryan',
        lastName: 'Murphy',
        location: 'Brooklyn, New York',
        registrationDate: new Date(Date.now() - 1000 * 60 * 60 * 24 * 400).toISOString() // 400 days ago
      };
      localStorage.setItem('user', JSON.stringify(mockUser));
    },
    template: '<ProfilePage />',
  }),
};