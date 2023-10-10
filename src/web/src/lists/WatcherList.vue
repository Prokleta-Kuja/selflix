<script setup lang="ts">
import { reactive, watch } from "vue";
import Search from '@/components/form/SearchBox.vue'
import { Header, Pages, Sizes, type ITableParams, initParams, getQuery, updateParams } from "@/components/table"
import ConfirmationModal from "@/components/ConfirmationModal.vue";
import { WatcherService, type WatcherLM, type WatcherVM } from "@/api";
import PlusIcon from '@/components/icons/PlusIcon.vue'
import TrashIcon from '@/components/icons/TrashIcon.vue'
import { useRoute, useRouter } from "vue-router";
import AddWatcherModal from "@/modals/AddWatcherModal.vue";
import EditWatcherModal from "@/modals/EditWatcherModal.vue";

interface IWatcherParams extends ITableParams {
    searchTerm?: string;
}
const route = useRoute()
const router = useRouter()
const props = defineProps<{ lastChange?: Date, queryPrefix?: string, userId?: number }>()
const data = reactive<{ params: IWatcherParams, items: WatcherLM[], adding?: boolean, edit?: WatcherLM, delete?: WatcherLM }>({ params: initParams(route.query, props.queryPrefix), items: [] });
const refresh = (params?: ITableParams) => {
    if (params)
        data.params = params;

    const query = { ...route.query, ...getQuery(data.params, props.queryPrefix) }
    router.replace({ query });

    WatcherService.getAllWatchers({ ...data.params, userId: props.userId })
        .then(r => {
            data.items = r.items;
            updateParams(data.params, r)
        });
};
const showAdd = () => data.adding = true
const showEdit = (item: WatcherLM) => data.edit = item;
const showDelete = (item: WatcherLM) => data.delete = item;
const hideDelete = () => data.delete = undefined;
const deleteItem = () => {
    if (!data.delete)
        return;

    WatcherService.deleteWatcher({ watcherId: data.delete.id })
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

const handleAdded = (item?: WatcherVM) => {
    if (item)
        data.items.unshift(item)
    data.adding = false
}

const handleUpdated = (item?: WatcherVM) => {
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
        <Search autoFocus class="me-3 mb-2" style="max-width:16rem" placeholder="Name" v-model="data.params.searchTerm"
            :on-change="resetPageNumber" />
    </div>
    <div class="table-responsive">
        <table class="table table-sm">
            <thead>
                <tr>
                    <Header :params="data.params" :on-sort="refresh" column="name" />
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
    <AddWatcherModal v-if="data.adding" @added="handleAdded" />
    <EditWatcherModal v-if="data.edit" :model="data.edit" @updated="handleUpdated" />
    <ConfirmationModal v-if="data.delete" title="Watcher deletion" @close="hideDelete" :onConfirm="deleteItem" shown>
        Are you sure you want to permanently delete <b>{{ data.delete.name }}</b>?
    </ConfirmationModal>
</template>