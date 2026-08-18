import type { Meta, StoryObj } from '@storybook/vue3';
import SentCode from '../../../views/Login/SentCodePage.vue';
import { vueRouter } from 'storybook-vue3-router';

const meta: Meta<typeof SentCode> = {
  title: 'Views/Auth/SentCode',
  component: SentCode,
  decorators: [vueRouter()],
  parameters: { layout: 'fullscreen' },
};

export default meta;
type Story = StoryObj<typeof SentCode>;

export const Default: Story = {};