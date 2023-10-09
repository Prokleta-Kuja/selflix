<script setup lang="ts">
import { reactive, watch } from "vue";
import Search from '@/components/form/SearchBox.vue'
import { Header, Pages, Sizes, type ITableParams, initParams, getQuery, updateParams } from "@/components/table"
import ConfirmationModal from "@/components/ConfirmationModal.vue";
import { UserDeviceService, type UserDeviceLM, type UserDeviceVM } from "@/api";
import PlusIcon from '@/components/icons/PlusIcon.vue'
import TrashIcon from '@/components/icons/TrashIcon.vue'
import { useRoute, useRouter } from "vue-router";
import { dateText } from "@/tools";
import AddUserDeviceModal from "@/modals/AddUserDeviceModal.vue";
import EditUserDeviceModal from "@/modals/EditUserDeviceModal.vue";

interface IUserDeviceParams extends ITableParams {
    searchTerm?: string;
}
const route = useRoute()
const router = useRouter()
const props = defineProps<{ lastChange?: Date, queryPrefix?: string, userId?: number }>()
const data = reactive<{ params: IUserDeviceParams, items: UserDeviceLM[], adding?: boolean, edit?: UserDeviceLM, delete?: UserDeviceLM }>({ params: initParams(route.query, props.queryPrefix), items: [] });
const refresh = (params?: ITableParams) => {
    if (params)
        data.params = params;

    const query = { ...route.query, ...getQuery(data.params, props.queryPrefix) }
    router.replace({ query });

    UserDeviceService.getAllDevices({ ...data.params, userId: props.userId })
        .then(r => {
            data.items = r.items;
            updateParams(data.params, r)
        });
};
const showAdd = () => data.adding = true
const showEdit = (item: UserDeviceLM) => data.edit = item;
const showDelete = (item: UserDeviceLM) => data.delete = item;
const hideDelete = () => data.delete = undefined;
const deleteItem = () => {
    if (!data.delete)
        return;

    UserDeviceService.deleteUserDevice({ userDeviceId: data.delete.id })
        .then(() => {
            refresh();
            hideDelete();
        })
        .catch(() => {/* TODO: show error */ })
}
const resetPageNumber = () => {
    data.params.page = 1
    refresh()
}

const handleAdded = (item?: UserDeviceVM) => {
    if (item)
        data.items.unshift(item)
    data.adding = false
}

const handleUpdated = (item?: UserDeviceVM) => {
    if (item && data.edit)
        data.edit.name = item.name
    data.edit = undefined;
}

watch(() => props.lastChange, () => refresh());
watch(() => props.userId, () => refresh());

refresh();
</script>
<template>
    <div class="d-flex flex-wrap">
        <Sizes class="me-3 mb-2" style="max-width:8rem" :params="data.params" :on-change="refresh" />
        <Search autoFocus class="me-3 mb-2" style="max-width:16rem" placeholder="Device name, brand, model"
            v-model="data.params.searchTerm" :on-change="resetPageNumber" />
    </div>
    <div class="table-responsive">
        <table class="table table-sm">
            <thead>
                <tr>
                    <Header :params="data.params" :on-sort="refresh" column="name" />
                    <Header :params="data.params" :on-sort="refresh" column="brand" />
                    <Header :params="data.params" :on-sort="refresh" column="model" />
                    <Header :params="data.params" :on-sort="refresh" column="os" />
                    <Header :params="data.params" :on-sort="refresh" column="created" />
                    <Header :params="data.params" :on-sort="refresh" column="lastLogin" display="Last login" />
                    <th class="text-end p-1">
                        <button class="btn btn-success btn-sm" @click="showAdd">
                            <PlusIcon />
                        </button>
                    </th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="item in data.items" :key="item.id" class="align-middle">
                    <td>
                        <a href="#" @click.prevent="showEdit(item)">
                            {{ item.name }}
                        </a>
                    </td>
                    <td>{{ item.brand }}</td>
                    <td>{{ item.model }}</td>
                    <td>{{ item.os }}</td>
                    <td>{{ dateText(item.created) }}</td>
                    <td>{{ dateText(item.lastLogin) }}</td>
                    <td class="text-end p-1">
                        <div class="btn-group" role="group">
                            <button class="btn btn-sm btn-outline-danger" title="Delete" @click="showDelete(item)">
                                <TrashIcon />
                            </button>
                        </div>
                    </td>
                </tr>
            </tbody>
        </table>
    </div>
    <Pages class="mb-2" :params="data.params" :on-change="refresh" />
    <AddUserDeviceModal v-if="data.adding" @added="handleAdded" />
    <EditUserDeviceModal v-if="data.edit" :model="data.edit" @updated="handleUpdated" />
    <ConfirmationModal v-if="data.delete" title="User device deletion" @close="hideDelete" :onConfirm="deleteItem" shown>
        Are you sure you want to permanently delete <b>{{ data.delete.name }}</b>?
    </ConfirmationModal>
</template>