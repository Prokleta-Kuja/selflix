<script setup lang="ts">
import { reactive, watch } from "vue";
import Search from '@/components/form/SearchBox.vue'
import { Header, Pages, Sizes, type ITableParams, initParams, getQuery, updateParams } from "@/components/table"
import ConfirmationModal from "@/components/ConfirmationModal.vue";
import { UserService, type UserLM } from "@/api";
import XLgIcon from '@/components/icons/XLgIcon.vue'
import CheckLgIcon from '@/components/icons/CheckLgIcon.vue'
import TrashIcon from '@/components/icons/TrashIcon.vue'
import { useRoute, useRouter } from "vue-router";
import { dateText } from "@/tools";

interface IUserParams extends ITableParams {
    searchTerm?: string;
}
const route = useRoute()
const router = useRouter()
const props = defineProps<{ lastChange?: Date, queryPrefix?: string }>()
const data = reactive<{ params: IUserParams, items: UserLM[], delete?: UserLM }>({ params: initParams(route.query, props.queryPrefix), items: [] });
const refresh = (params?: ITableParams) => {
    if (params)
        data.params = params;

    const query = { ...route.query, ...getQuery(data.params, props.queryPrefix) }
    router.replace({ query });

    UserService.getUsers({ ...data.params })
        .then(r => {
            data.items = r.items;
            updateParams(data.params, r)
        });
};
const showDelete = (item: UserLM) => data.delete = item;
const hideDelete = () => data.delete = undefined;
const deleteItem = () => {
    if (!data.delete)
        return;

    UserService.deleteUser({ userId: data.delete.id })
        .then(() => {
            refresh();
            hideDelete();
        })
        .catch(() => {/* TODO: show error */ })
}
const disableItem = (item: UserLM) => {
    UserService.toggleDisabled({ userId: item.id })
        .then(() => refresh())
        .catch(() => {/* TODO: show error */ })
}
const resetPageNumber = () => {
    data.params.page = 1
    refresh()
}

watch(() => props.lastChange, () => refresh());

refresh();
</script>
<template>
    <div class="d-flex flex-wrap">
        <Sizes class="me-3 mb-2" style="max-width:8rem" :params="data.params" :on-change="refresh" />
        <Search autoFocus class="me-3 mb-2" style="max-width:16rem" placeholder="User name" v-model="data.params.searchTerm"
            :on-change="resetPageNumber" />
    </div>
    <div class="table-responsive">
        <table class="table table-sm">
            <thead>
                <tr>
                    <Header :params="data.params" :on-sort="refresh" column="name" />
                    <Header :params="data.params" :on-sort="refresh" column="isAdmin" display="Admin" />
                    <Header :params="data.params" :on-sort="refresh" column="disabled" display="Disabled" />
                    <th></th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="item in data.items" :key="item.id" class="align-middle">
                    <td>
                        <RouterLink :to="{ name: 'user-devices', params: { id: item.id } }">{{ item.name }}
                        </RouterLink>
                    </td>
                    <td>
                        <CheckLgIcon v-if="item.isAdmin" class="text-success" />
                        <XLgIcon v-else class="text-danger" />
                    </td>
                    <td>{{ dateText(item.disabled) }}</td>
                    <td class="text-end p-1">
                        <div class="btn-group" role="group">
                            <button class="btn btn-sm btn-outline-danger" title="Delete" @click="showDelete(item)">
                                <TrashIcon />
                            </button>
                            <button v-if="item.disabled" class="btn btn-sm btn-success" title="Enable"
                                @click="disableItem(item)">
                                <CheckLgIcon />
                            </button>
                            <button v-else class="btn btn-sm btn-danger" title="Disable" @click="disableItem(item)">
                                <XLgIcon />
                            </button>
                        </div>
                    </td>
                </tr>
            </tbody>
        </table>
    </div>
    <Pages class="mb-2" :params="data.params" :on-change="refresh" />
    <ConfirmationModal v-if="data.delete" title="User deletion" :onClose="hideDelete" :onConfirm="deleteItem" shown>
        Are you sure you want to permanently delete <b>{{ data.delete.name }}</b>?
    </ConfirmationModal>
</template>