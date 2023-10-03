<script setup lang="ts">
import UserList from '@/lists/UserList.vue';

</script>

<template>
    <main>
        <h1 class="display-6 me-3">Users</h1>
        <UserList query-prefix="first" />
        <UserList query-prefix="second" />
    </main>
</template>