<script setup lang="ts">
import { ref, reactive } from 'vue';
import GenericModal from '@/components/GenericModal.vue';
import type IModelState from './form/modelState';
import { AuthService, type TotpCM, type TotpVM } from '@/api';
import IntegerBox from './form/IntegerBox.vue';
import SpinButton from './form/SpinButton.vue';
import { useAuth } from '@/stores/auth';

const auth = useAuth()
const shown = ref(false)
const view = reactive<IModelState<TotpVM>>({ model: { chunkedSecret: '', qr: '' } })
const create = reactive<IModelState<TotpCM>>({ model: { chunkedSecret: '' } })
const hide = () => shown.value = false;
const show = () => {
    view.loading = true;
    view.error = undefined;
    AuthService.getTotp()
        .then(r => {
            view.model.qr = r.qr;
            view.model.chunkedSecret = r.chunkedSecret;
            create.model.chunkedSecret = r.chunkedSecret;
            view.loading = false;
        })
        .catch(r => view.error = r.body)
        .finally(() => shown.value = true);
}
const copySecret = () => navigator.clipboard.writeText(view.model.chunkedSecret);
const submit = () => {
    create.submitting = true;
    create.error = undefined;
    AuthService.saveTotp({ requestBody: create.model })
        .then(() => {
            auth.hasOtp = true;
            hide();
        })
        .catch(r => create.error = r.body)
        .finally(() => create.submitting = false);
}

</script>
<template>
    <div class="nav-link py-2 px-0 px-lg-2 pointer" title="Add TOTP" @click="show">
        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="bi bi-qr-code text-danger"
            viewBox="0 0 16 16">
            <path d="M2 2h2v2H2V2Z" />
            <path d="M6 0v6H0V0h6ZM5 1H1v4h4V1ZM4 12H2v2h2v-2Z" />
            <path d="M6 10v6H0v-6h6Zm-5 1v4h4v-4H1Zm11-9h2v2h-2V2Z" />
            <path
                d="M10 0v6h6V0h-6Zm5 1v4h-4V1h4ZM8 1V0h1v2H8v2H7V1h1Zm0 5V4h1v2H8ZM6 8V7h1V6h1v2h1V7h5v1h-4v1H7V8H6Zm0 0v1H2V8H1v1H0V7h3v1h3Zm10 1h-1V7h1v2Zm-1 0h-1v2h2v-1h-1V9Zm-4 0h2v1h-1v1h-1V9Zm2 3v-1h-1v1h-1v1H9v1h3v-2h1Zm0 0h3v1h-2v1h-1v-2Zm-4-1v1h1v-2H7v1h2Z" />
            <path d="M7 12h1v3h4v1H7v-4Zm9 2v2h-3v-1h2v-1h1Z" />
        </svg>
        <small class="d-lg-none ms-2">Add TOTP</small>
    </div>
    <GenericModal title="Time-based One-Time Password" :onClose="hide" :shown=shown>
        <template #body>
            <template v-if="view.loading">
                <div class="text-center mt-5">
                    <div class="spinner-border" role="status"></div>
                </div>
            </template>
            <span v-else-if="view.error" class="text-danger">{{ view.error }}</span>
            <template v-else>
                <span>Scan the following QR code with authenticator app or use the secret bellow to add to your
                    password manager.</span>
                <img class="rounded mx-auto d-block mt-3" :src="view.model.qr" />

                <label class="form-label">Secret code</label>
                <div class="input-group mb-3">
                    <input type="text" class="form-control" :value="create.model.chunkedSecret" readonly>
                    <button class="btn btn-outline-secondary" type="button" title="Copy to clipboard" @click="copySecret">
                        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor"
                            class="bi bi-clipboard-fill" viewBox="0 0 16 16">
                            <path fill-rule="evenodd"
                                d="M10 1.5a.5.5 0 0 0-.5-.5h-3a.5.5 0 0 0-.5.5v1a.5.5 0 0 0 .5.5h3a.5.5 0 0 0 .5-.5v-1Zm-5 0A1.5 1.5 0 0 1 6.5 0h3A1.5 1.5 0 0 1 11 1.5v1A1.5 1.5 0 0 1 9.5 4h-3A1.5 1.5 0 0 1 5 2.5v-1Zm-2 0h1v1A2.5 2.5 0 0 0 6.5 5h3A2.5 2.5 0 0 0 12 2.5v-1h1a2 2 0 0 1 2 2V14a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V3.5a2 2 0 0 1 2-2Z" />
                        </svg>
                    </button>
                </div>
                <form @submit.prevent="submit">
                    <IntegerBox class="mb-3" label="One Time Code" autoComplete="one-time-code" v-model="create.model.code"
                        required help="Enter the code from authenticator app for the added secret"
                        :error="create.error?.errors?.code" />
                </form>
            </template>
        </template>
        <template #footer>
            <p v-if="create.error" class="text-danger">{{ create.error.message }}</p>
            <button class="btn btn-outline-danger" @click="hide">Cancel</button>
            <SpinButton class="btn-primary" :loading="create.submitting" text="Save" loadingText="Saving" @click="submit" />
        </template>
    </GenericModal>
</template>