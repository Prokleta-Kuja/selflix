<script setup lang="ts">
import { reactive } from 'vue';
import { type UserDeviceUM, UserDeviceService, type UserDeviceVM } from '@/api';
import type IModelState from '@/components/form/modelState';
import Modal from '@/components/GenericModal.vue';
import SpinButton from '@/components/form/SpinButton.vue';
import Text from '@/components/form/TextBox.vue';
import CheckBox from '@/components/form/CheckBox.vue';
export interface IEditUserDevice {
    model: UserDeviceVM,
    onUpdated?: (updatedItem?: UserDeviceVM) => void
}

const mapItemModel = (m: UserDeviceVM): UserDeviceUM =>
({
    name: m.name,
})
const props = defineProps<IEditUserDevice>()
const item = reactive<IModelState<UserDeviceUM>>({ model: mapItemModel(props.model) })

const close = () => {
    if (props.onUpdated)
        props.onUpdated()
}

const submit = () => {
    item.submitting = true;
    item.error = undefined;
    UserDeviceService.updateUserDevice({ userDeviceId: props.model.id, requestBody: item.model })
        .then(r => {
            if (props.onUpdated)
                props.onUpdated(r);
        })
        .catch(r => item.error = r.body)
        .finally(() => item.submitting = false);
};
</script>
<template>
    <Modal v-if="item.model" title="Edit user device" shown :onClose="close">
        <template #body>
            <form @submit.prevent="submit">
                <Text class="mb-3" label="Name" autoFocus v-model="item.model.name" required
                    :error="item.error?.errors?.name" />
                <CheckBox class="mb-3" label="Clear Auth Key" v-model="item.model.clearOtpKey"
                    :error="item.error?.errors?.clearOtpKey" />
            </form>
        </template>
        <template #footer>
            <p v-if="item.error" class="text-danger">{{ item.error.message }}</p>
            <button class="btn btn-outline-danger" @click="close">Cancel</button>
            <SpinButton class="btn-primary" :loading="item.submitting" text="Save" loadingText="Saving" @click="submit" />
        </template>
    </Modal>
</template>