import type { LocationQuery } from 'vue-router'
import Header from './TableHeader.vue'
import Pages from './TablePages.vue'
import Sizes from './TableSizes.vue'
import { nameof } from '@/tools'

export { Header }
export { Pages }
export { Sizes }

export interface IListResponse<T> {
  items: Array<T>
  size: number
  page: number
  total: number
  ascending: boolean
  sortBy?: string
}

export interface ITableParams {
  size: number
  page: number
  total: number
  ascending: boolean
  sortBy?: string
}

const getQueryKeys = (prefix?: string) => ({
  size: prefix ? `${prefix}.${nameof<ITableParams>("size")}` : nameof<ITableParams>("size"),
  page: prefix ? `${prefix}.${nameof<ITableParams>("page")}` : nameof<ITableParams>("page"),
  ascending: prefix ? `${prefix}.${nameof<ITableParams>("ascending")}` : nameof<ITableParams>("ascending"),
  sortBy: prefix ? `${prefix}.${nameof<ITableParams>("sortBy")}` : nameof<ITableParams>("sortBy"),
})

export const initParams = (query?: LocationQuery, prefix?: string): ITableParams => {
  const params: ITableParams = { page: 1, size: 25, total: 0, ascending: false }
  if (query) {
    const keys = getQueryKeys(prefix);
    if (keys.size in query && query[keys.size])
      try {
        const val = query[keys.size]!.toString();
        params.size = parseInt(val);
      } catch (e) {
        console.error(`Couldn't parse query param ${keys.size}`, e);
      }
    if (keys.page in query && query[keys.page])
      try {
        const val = query[keys.page]!.toString();
        params.page = parseInt(val);
      } catch (e) {
        console.error(`Couldn't parse query param ${keys.page}`, e);
      }
    if (keys.sortBy in query && query[keys.sortBy])
      params.sortBy = query[keys.page]!.toString()
    params.ascending = keys.ascending in query;
  }

  return params
}

export const getQuery = (params: ITableParams, prefix?: string): LocationQuery => {
  const keys = getQueryKeys(prefix);
  const query = { [keys.size]: params.size.toString(), [keys.page]: params.page.toString() };
  if (params.sortBy)
    query[keys.sortBy] = params.sortBy;
  if (params.ascending)
    query[keys.ascending] = '1'

  return query;
}

export const updateParams = (
  params: ITableParams,
  response: {
    size: number
    page: number
    total: number
    ascending: boolean
    sortBy?: string | null
  }
) => {
  if (params.size !== response.size) params.size = response.size
  if (params.page !== response.page) params.page = response.page
  if (params.total !== response.total) params.total = response.total
  if (params.ascending !== response.ascending) params.ascending = response.ascending
  if (params.sortBy !== response.sortBy)
    params.sortBy = response.sortBy === null ? undefined : response.sortBy
}

export const defaultPageSizes = [10, 25, 50, 100]