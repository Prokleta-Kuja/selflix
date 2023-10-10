<script setup lang="ts">
import { reactive } from 'vue';
import { type WatcherUM, WatcherService, type WatcherVM } from '@/api';
import type IModelState from '@/components/form/modelState';
import Modal from '@/components/GenericModal.vue';
import SpinButton from '@/components/form/SpinButton.vue';
import Text from '@/components/form/TextBox.vue';
import HiddenSubmit from '@/components/form/HiddenSubmit.vue'

export interface IEditWatcher {
    model: WatcherVM,
    onUpdated?: (updatedItem?: WatcherVM) => void
}

const mapItemModel = (m: WatcherVM): WatcherUM =>
({
    name: m.name,
})
const props = defineProps<IEditWatcher>()
const item = reactive<IModelState<WatcherUM>>({ model: mapItemModel(props.model) })

const close = () => {
    if (props.onUpdated)
        props.onUpdated()
}

const submit = () => {
    item.submitting = true;
    item.error = undefined;
    WatcherService.updateWatcher({ watcherId: props.model.id, requestBody: item.model })
        .then(r => {
            if (props.onUpdated)
                props.onUpdated(r);
        })
        .catch(r => item.error = r.body)
        .finally(() => item.submitting = false);
};
</script>
<template>
    <Modal v-if="item.model" title="Edit watcher" shown :onClose="close">
        <template #body>
            <form @submit.prevent="submit">
                <Text class="mb-3" label="Name" autoFocus v-model="item.model.name" required
                    :error="item.error?.errors?.name" />
                <HiddenSubmit />
            </form>
        </template>
        <template #footer>
            <p v-if="item.error" class="text-danger">{{ item.error.message }}</p>
            <button class="btn btn-outline-danger" @click="close">Cancel</button>
            <SpinButton class="btn-primary" :loading="item.submitting" text="Save" loadingText="Saving" @click="submit" />
        </template>
    </Modal>
</template>