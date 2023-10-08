<script setup lang="ts">
import UserList from '@/lists/UserList.vue';
import AddUserModal from '@/modals/AddUserModal.vue';
</script>

<template>
    <main>
        <div class="d-flex align-items-center flex-wrap">
            <h1 class="display-6 me-3">Users</h1>
            <AddUserModal />
        </div>
        <UserList />
    </main>
</template>