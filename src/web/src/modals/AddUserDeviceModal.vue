<script setup lang="ts">
import { reactive, ref } from 'vue';
import { type UserDeviceCM, UserDeviceService, type UserDeviceVM } from '@/api';
import type IModelState from '@/components/form/modelState';
import Modal from '@/components/GenericModal.vue';
import SpinButton from '@/components/form/SpinButton.vue';
import Text from '@/components/form/TextBox.vue';
import PlusLgIcon from '@/components/icons/PlusLgIcon.vue'

const blank = (): UserDeviceCM => ({ name: '', deviceId: '' })
const shown = ref(false)
const item = reactive<IModelState<UserDeviceCM>>({ model: blank() })
const emit = defineEmits<{ (e: 'added', item?: UserDeviceVM): void }>()

const toggle = () => shown.value = !shown.value
const submit = () => {
    item.submitting = true;
    item.error = undefined;
    UserDeviceService.createUserDevice({ requestBody: item.model })
        .then(r => {
            emit('added', r)
            shown.value = false
        })
        .catch(r => item.error = r.body)
        .finally(() => item.submitting = false);
};
</script>
<template>
    <button class="btn btn-success btn-sm" @click="toggle">
        <PlusLgIcon />
    </button>
    <Modal title="Add device" :shown="shown" :onClose="toggle">
        <template #body>
            <form @submit.prevent="submit">
                <Text class="mb-3" label="Name" autoFocus v-model="item.model.name" required
                    :error="item.error?.errors?.name" />
                <Text class="mb-3" label="DeviceId" autoFocus v-model="item.model.deviceId" required
                    :error="item.error?.errors?.deviceId" />
            </form>
        </template>
        <template #footer>
            <p v-if="item.error" class="text-danger">{{ item.error.message }}</p>
            <button class="btn btn-outline-danger" @click="toggle">Cancel</button>
            <SpinButton class="btn-primary" :loading="item.submitting" text="Add" loadingText="Adding" @click="submit" />
        </template>
    </Modal>
</template>