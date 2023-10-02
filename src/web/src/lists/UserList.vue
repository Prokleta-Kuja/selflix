<script setup lang="ts">
import { reactive, watch } from "vue";
import Search from '@/components/form/SearchBox.vue'
import { Header, Pages, Sizes, type ITableParams, initParams, getQuery, updateParams } from "@/components/table"
import ConfirmationModal from "@/components/ConfirmationModal.vue";
import { UserService, type UserLM } from "@/api";
import XLg from '@/components/icons/XLg.vue'
import CheckLg from '@/components/icons/CheckLg.vue'
import { useRoute, useRouter } from "vue-router";

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

    const query = { ...getQuery, ...getQuery(data.params, props.queryPrefix) }
    router.push({ query });
    UserService.getUsers({ ...data.params })
        .then(r => {
            data.items = r.items;
            updateParams(data.params, r)
            scrollTo(0, 0)
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
        <Search autoFocus class="me-3 mb-2" style="max-width:16rem" placeholder="User name" v-model="data.params.searchTerm"
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
                        <RouterLink :to="{ name: 'user', params: { id: item.id } }">{{ item.name }}
                        </RouterLink>
                    </td>
                    <td>
                        <CheckLg v-if="item.isAdmin" class="text-success" />
                        <XLg v-else class="text-danger" />
                    </td>
                    <td>{{ disabledText(item.disabled) }}</td>
                    <td class="text-end p-1">
                        <div class="btn-group" role="group">
                            <button class="btn btn-sm btn-danger" title="Delete" @click="showDelete(item)">
                                <XLg />
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