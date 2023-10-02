<script setup lang="ts">
import { reactive, watch } from "vue";
import Search from '@/components/form/SearchBox.vue'
import { Header, Pages, Sizes, type ITableParams, initParams, updateParams } from "@/components/table"
import ConfirmationModal from "@/components/ConfirmationModal.vue";
import { UserService, type UserLM } from "@/api";
import XLg from '@/components/icons/XLg.vue'
import CheckLg from '@/components/icons/CheckLg.vue'

interface IUserParams extends ITableParams {
    searchTerm?: string;
}

const props = defineProps<{ lastChange?: Date }>()
const data = reactive<{ params: IUserParams, items: UserLM[], delete?: UserLM }>({ params: initParams(), items: [] });
const refresh = (params?: ITableParams) => {
    if (params)
        data.params = params;

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

watch(() => props.lastChange, () => refresh());

const disabledText = (dateTime: string | null | undefined) => {
    if (!dateTime)
        return '-';
    var dt = new Date(dateTime);
    return dt.toLocaleString();
}

refresh();
</script>
<template>
    <div class="d-flex flex-wrap">
        <Sizes class="me-3 mb-2" style="max-width:8rem" :params="data.params" :on-change="refresh" />
        <Search autoFocus class="me-3 mb-2" style="max-width:16rem" placeholder="Pattern" v-model="data.params.searchTerm"
            :on-change="refresh" />
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
                        <RouterLink :to="{ name: 'route.userDetails', params: { id: item.id } }">{{ item.name }}
                        </RouterLink>
                    </td>
                    <td>
                        <CheckLg v-if="item.isAdmin" class="text-success" />
                        <XLg v-else class="text-danger" />
                    </td>
                    <td>{{ disabledText(item.disabled) }}</td>
                    <td class="text-end p-1">
                        <template>
                            <div class="btn-group" role="group">
                                <button class="btn btn-sm btn-danger" title="Delete" @click="showDelete(item)">
                                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor"
                                        class="bi bi-x-lg" viewBox="0 0 16 16">
                                        <path
                                            d="M2.146 2.854a.5.5 0 1 1 .708-.708L8 7.293l5.146-5.147a.5.5 0 0 1 .708.708L8.707 8l5.147 5.146a.5.5 0 0 1-.708.708L8 8.707l-5.146 5.147a.5.5 0 0 1-.708-.708L7.293 8 2.146 2.854Z" />
                                    </svg>
                                </button>
                            </div>
                        </template>
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