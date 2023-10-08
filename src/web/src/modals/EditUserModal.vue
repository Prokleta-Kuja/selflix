<script setup lang="ts">
import { reactive, ref } from 'vue';
import { type UserUM, UserService, type UserVM } from '@/api';
import type IModelState from '@/components/form/modelState';
import Modal from '@/components/GenericModal.vue';
import SpinButton from '@/components/form/SpinButton.vue';
import PencilSquareIcon from '@/components/icons/PencilSquareIcon.vue'
import Text from '@/components/form/TextBox.vue';
import CheckBox from '@/components/form/CheckBox.vue';
export interface IEditUser {
    model: UserVM,
    onUpdated?: (updatedDomain: UserVM) => void
}

const mapUserModel = (m: UserVM): UserUM =>
({
    name: m.name,
    isAdmin: m.isAdmin,
    disabled: m.disabled ? true : false,
})
const props = defineProps<IEditUser>()
const shown = ref(false)
const item = reactive<IModelState<UserUM>>({ model: mapUserModel(props.model) })

const toggle = () => shown.value = !shown.value
const submit = () => {
    item.submitting = true;
    item.error = undefined;
    UserService.updateUser({ userId: props.model.id, requestBody: item.model })
        .then(r => {
            shown.value = false;
            if (props.onUpdated)
                props.onUpdated(r);
        })
        .catch(r => item.error = r.body)
        .finally(() => item.submitting = false);
};
</script>
<template>
    <button class="btn btn-primary me-3" @click="toggle">
        <PencilSquareIcon />
    </button>
    <Modal v-if="item.model" title="Edit user" :shown="shown" :onClose="toggle">
        <template #body>
            <form @submit.prevent="submit">
                <Text class="mb-3" label="Username" autoFocus v-model="item.model.name" required
                    :error="item.error?.errors?.name" />
                <Text class="mb-3" label="Replace password" :autoComplete="'off'" :type="'password'"
                    v-model="item.model.newPassword" :error="item.error?.errors?.newPassword" />
                <CheckBox class="mb-3" label="Admin" v-model="item.model.isAdmin" :error="item.error?.errors?.isAdmin" />
                <CheckBox class="mb-3" label="Disabled" v-model="item.model.disabled"
                    :error="item.error?.errors?.disabled" />
                <CheckBox class="mb-3" label="Clear One Time Code Key" v-model="item.model.clearOtpKey"
                    :error="item.error?.errors?.clearOtpKey" />
            </form>
        </template>
        <template #footer>
            <p v-if="item.error" class="text-danger">{{ item.error.message }}</p>
            <button class="btn btn-outline-danger" @click="toggle">Cancel</button>
            <SpinButton class="btn-primary" :loading="item.submitting" text="Save" loadingText="Saving" @click="submit" />
        </template>
    </Modal>
</template>