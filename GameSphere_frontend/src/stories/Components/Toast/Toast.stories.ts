import type { Meta, StoryObj } from '@storybook/vue3';
import Toast from '../../../components/ui/custom/Toast/Toast.vue';
import { ref } from 'vue';

const meta: Meta<typeof Toast> = {
  title: 'Components/UI/Toast',
  component: Toast,
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
  },
};

export default meta;
type Story = StoryObj<typeof Toast>;

export const Success: Story = {
  render: (args) => ({
    components: { Toast },
    setup() {
      const toastRef = ref();
      const trigger = () => {
        toastRef.value.showToast({
          title: 'Success!',
          description: 'Your action was completed successfully.',
          variant: 'success'
        });
      };
      return { toastRef, trigger };
    },
    template: `
      <div>
        <button @click="trigger" class="px-4 py-2 bg-green-600 text-white rounded">Trigger Success</button>
        <Toast ref="toastRef" v-bind="args" />
      </div>
    `,
  }),
};

export const Error: Story = {
  render: (args) => ({
    components: { Toast },
    setup() {
      const toastRef = ref();
      const trigger = () => {
        toastRef.value.showToast({
          title: 'Error Occurred',
          description: 'Failed to connect to the GameSphere API.',
          variant: 'error'
        });
      };
      return { toastRef, trigger };
    },
    template: `
      <div>
        <button @click="trigger" class="px-4 py-2 bg-red-600 text-white rounded">Trigger Error</button>
        <Toast ref="toastRef" v-bind="args" />
      </div>
    `,
  }),
};