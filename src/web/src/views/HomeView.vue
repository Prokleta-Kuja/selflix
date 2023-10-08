<script setup lang="ts">
import { UserService, type PlainError, type UserVM } from '@/api';
import DeviceList from '@/lists/DeviceList.vue';
import EditUserModal from '@/modals/EditUserModal.vue';
import { reactive, watch } from 'vue';

const props = defineProps<{ id?: number }>()
const user = reactive<{ error?: PlainError, value?: UserVM }>({});
const updateUser = (updatedUser: UserVM) => user.value = updatedUser;
const loadUser = () => {
  if (props.id)
    UserService.getUser({ userId: props.id })
      .then(r => user.value = r)
      .catch(r => user.error = r.body);
  else {
    user.value = undefined
    user.error = undefined
  }
}

watch(() => props.id, loadUser);

loadUser()
</script>

<template>
  <main>
    <div class="d-flex align-items-center flex-wrap">
      <h1 v-if="props.id" class="display-6 me-3">
        <template v-if="user.value">
          {{ user.value.name }} devices
        </template>
        <template v-else>
          User devices
        </template>
      </h1>
      <h1 v-else class="display-6 me-3">My devices</h1>
      <EditUserModal v-if="user.value" :model="user.value" @updated="updateUser" />
    </div>
    <DeviceList :user-id="props.id" />
  </main>
</template>
