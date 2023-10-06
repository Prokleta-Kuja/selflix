<script setup lang="ts">
import { useAuth } from '@/stores/auth';


const auth = useAuth()
</script>

<template>
    <main>
        <h1 class="display-6 me-3"><template v-if="auth.isAdmin">User </template>Devices</h1>
    </main>
</template>