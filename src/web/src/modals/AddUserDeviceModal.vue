<script setup lang="ts">
import { reactive } from 'vue';
import { type UserDeviceCM, UserDeviceService, type UserDeviceVM } from '@/api';
import type IModelState from '@/components/form/modelState';
import Modal from '@/components/GenericModal.vue';
import SpinButton from '@/components/form/SpinButton.vue';
import Text from '@/components/form/TextBox.vue';

const blank = (): UserDeviceCM => ({ name: '', deviceId: '' })
const props = defineProps<{ onAdded?: (addedItem?: UserDeviceVM) => void }>()
const item = reactive<IModelState<UserDeviceCM>>({ model: blank() })
const emit = defineEmits<{ (e: 'added', item?: UserDeviceVM): void }>()

const close = () => {
    if (props.onAdded)
        props.onAdded()
}
const submit = () => {
    item.submitting = true;
    item.error = undefined;
    UserDeviceService.createUserDevice({ requestBody: item.model })
        .then(r => {
            emit('added', r)
        })
        .catch(r => item.error = r.body)
        .finally(() => item.submitting = false);
};
</script>
<template>
    <Modal title="Add device" shown :onClose="close">
        <template #body>
            <form @submit.prevent="submit">
                <Text class="mb-3" label="Name" autoFocus v-model="item.model.name" required
                    :error="item.error?.errors?.name" />
                <Text class="mb-3" label="DeviceId" v-model="item.model.deviceId" required
                    :error="item.error?.errors?.deviceId" />
            </form>
        </template>
        <template #footer>
            <p v-if="item.error" class="text-danger">{{ item.error.message }}</p>
            <button class="btn btn-outline-danger" @click="close">Cancel</button>
            <SpinButton class="btn-primary" :loading="item.submitting" text="Add" loadingText="Adding" @click="submit" />
        </template>
    </Modal>
</template>