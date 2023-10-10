<script setup lang="ts">
import { reactive, ref } from 'vue';
import { type UserCM, UserService } from '@/api';
import type IModelState from '@/components/form/modelState';
import Modal from '@/components/GenericModal.vue';
import SpinButton from '@/components/form/SpinButton.vue';
import Text from '@/components/form/TextBox.vue';
import CheckBox from '@/components/form/CheckBox.vue';
import { useRouter } from 'vue-router';
import PlusLgIcon from '@/components/icons/PlusLgIcon.vue'
import HiddenSubmit from '@/components/form/HiddenSubmit.vue'

const router = useRouter()
const blank = (): UserCM => ({ name: '', isAdmin: false, password: '' })
const shown = ref(false)
const item = reactive<IModelState<UserCM>>({ model: blank() })

const toggle = () => shown.value = !shown.value
const submit = () => {
    item.submitting = true;
    item.error = undefined;
    UserService.createUser({ requestBody: item.model })
        .then(r => router.push({ name: 'user-devices', params: { id: r.id } }))
        .catch(r => item.error = r.body)
        .finally(() => item.submitting = false);
};
</script>
<template>
    <button class="btn btn-success" @click="toggle">
        <PlusLgIcon />
        Add
    </button>
    <Modal title="Add user" :shown="shown" :onClose="toggle">
        <template #body>
            <form @submit.prevent="submit">
                <Text class="mb-3" label="Username" autoFocus v-model="item.model.name" required
                    :error="item.error?.errors?.name" />
                <Text class="mb-3" label="Password" :autoComplete="'off'" :type="'password'" v-model="item.model.password"
                    :error="item.error?.errors?.password" required />
                <CheckBox class="mb-3" label="Admin" v-model="item.model.isAdmin" :error="item.error?.errors?.isAdmin" />
                <HiddenSubmit />
            </form>
        </template>
        <template #footer>
            <p v-if="item.error" class="text-danger">{{ item.error.message }}</p>
            <button class="btn btn-outline-danger" @click="toggle">Cancel</button>
            <SpinButton class="btn-primary" :loading="item.submitting" text="Add" loadingText="Adding" @click="submit" />
        </template>
    </Modal>
</template>