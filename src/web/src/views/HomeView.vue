<script setup lang="ts">
import DeviceList from '@/lists/DeviceList.vue';

const props = defineProps<{ id?: number }>()
</script>

<template>
  <main>
    <h1 v-if="props.id" class="display-6 me-3">User devices</h1>
    <h1 v-else class="display-6 me-3">My devices</h1>
    <DeviceList :user-id="props.id" />
  </main>
</template>
