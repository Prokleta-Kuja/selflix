<script setup lang="ts">
import { reactive, watch } from "vue";
import Search from '@/components/form/SearchBox.vue'
import { Header, Pages, Sizes, type ITableParams, initParams, getQuery, updateParams } from "@/components/table"
import ConfirmationModal from "@/components/ConfirmationModal.vue";
import { LibraryService, type LibraryLM } from "@/api";
import ArrowRepeatIcon from '@/components/icons/ArrowRepeatIcon.vue'
import TrashIcon from '@/components/icons/TrashIcon.vue'
import { useRoute, useRouter } from "vue-router";
import { dateText } from "@/tools";

interface ILibraryParams extends ITableParams {
    searchTerm?: string;
}
const route = useRoute()
const router = useRouter()
const props = defineProps<{ lastChange?: Date, queryPrefix?: string }>()
const data = reactive<{ params: ILibraryParams, items: LibraryLM[], delete?: LibraryLM }>({ params: initParams(route.query, props.queryPrefix), items: [] });
const refresh = (params?: ITableParams) => {
    if (params)
        data.params = params;

    const query = { ...route.query, ...getQuery(data.params, props.queryPrefix) }
    router.replace({ query });

    LibraryService.getAllLibraries({ ...data.params })
        .then(r => {
            data.items = r.items;
            updateParams(data.params, r)
        });
};
const showDelete = (item: LibraryLM) => data.delete = item;
const hideDelete = () => data.delete = undefined;
const deleteItem = () => {
    if (!data.delete)
        return;

    LibraryService.deleteLibrary({ libraryId: data.delete.id })
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

const index = (item: LibraryLM) => {
    LibraryService.scheduleIndex({ libraryId: item.id, fullIndex: false })
        .then(() => refresh())
}

watch(() => props.lastChange, () => refresh());

refresh();
</script>
<template>
    <div class="d-flex flex-wrap">
        <Sizes class="me-3 mb-2" style="max-width:8rem" :params="data.params" :on-change="refresh" />
        <Search autoFocus class="me-3 mb-2" style="max-width:16rem" placeholder="Library name"
            v-model="data.params.searchTerm" :on-change="resetPageNumber" />
    </div>
    <div class="table-responsive">
        <table class="table table-sm">
            <thead>
                <tr>
                    <Header :params="data.params" :on-sort="refresh" column="name" />
                    <Header :params="data.params" :on-sort="refresh" column="mediaPath" />
                    <Header :params="data.params" :on-sort="refresh" column="lastFullIndexCompleted" display="Last index" />
                    <th></th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="item in data.items" :key="item.id" class="align-middle">
                    <td>{{ item.name }}</td>
                    <td>{{ item.mediaPath }}</td>
                    <td>
                        <div v-if="item.indexing" class="spinner-border spinner-border-sm" role="status">
                            <span class="visually-hidden">Indexing...</span>
                        </div>
                        <template v-else>
                            {{ dateText(item.lastFullIndexCompleted) }}
                        </template>
                    </td>
                    <td class="text-end p-1">
                        <div class="btn-group" role="group">
                            <button class="btn btn-sm btn-success" :disabled="item.indexing" @click="index(item)">
                                <ArrowRepeatIcon />
                            </button>
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
    <ConfirmationModal v-if="data.delete" title="Library deletion" :onClose="hideDelete" :onConfirm="deleteItem" shown>
        Are you sure you want to permanently delete <b>{{ data.delete.name }}</b>?
    </ConfirmationModal>
</template>