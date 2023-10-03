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

const sizeKey = nameof<ITableParams>('size')
const totalKey = nameof<ITableParams>('total')

const minPageSize = 10,
  defaultPageSize = 25,
  maxPageSize = 100
export const defaultPageSizes = [minPageSize, defaultPageSize, 50, maxPageSize]

export const getQueryKey = (key: string, prefix?: string) => (prefix ? `${prefix}.${key}` : key)
export const getParamsKey = (queryKey: string, prefix?: string) =>
  prefix ? queryKey.replace(`${prefix}.`, '') : queryKey

export const initParams = (query?: LocationQuery, prefix?: string): ITableParams => {
  const params: ITableParams = { page: 1, size: 25, total: 0, ascending: false }
  if (query) {
    const indexableParams = params as { [key: string]: any }
    const trimPrefix = prefix ? `${prefix}.` : ''

    Object.keys(query).forEach((queryKey) => {
      if (!queryKey.startsWith(trimPrefix)) return

      const key = getParamsKey(queryKey, prefix)

      if (query[queryKey] == null) {
        indexableParams[key] = true
        return
      }
      let val = parseInt(query[queryKey]!.toString())
      if (isNaN(val)) val = parseFloat(query[queryKey]!.toString())
      if (!isNaN(val)) {
        indexableParams[key] = val
        return
      }

      indexableParams[key] = query[queryKey]!.toString()
    })
  }

  if (sizeKey in params) {
    const indexableParams = params as { [key: string]: any }
    const val = parseInt(indexableParams[sizeKey])
    if (isNaN(val)) indexableParams[sizeKey] = defaultPageSize
    else if (val < minPageSize) indexableParams[sizeKey] = minPageSize
    else if (val > maxPageSize) indexableParams[sizeKey] = maxPageSize
    else if (!defaultPageSizes.some((size) => size === val))
      indexableParams[sizeKey] = defaultPageSize
  }

  return params
}

export const getQuery = (params: ITableParams, prefix?: string): LocationQuery => {
  const query: { [key: string]: string | null } = {}
  const indexableParams = params as { [key: string]: any }
  Object.keys(params).forEach((key) => {
    if (key === totalKey) return

    const queryKey = getQueryKey(key, prefix)
    const type = typeof indexableParams[key]
    if (type === 'boolean')
      if (indexableParams[key]) {
        query[queryKey] = null
        return
      } else return

    query[queryKey] = indexableParams[key]
  })

  return query
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
