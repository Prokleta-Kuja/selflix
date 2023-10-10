/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { WatcherCM } from '../models/WatcherCM';
import type { WatcherLMListResponse } from '../models/WatcherLMListResponse';
import type { WatcherUM } from '../models/WatcherUM';
import type { WatcherVM } from '../models/WatcherVM';

import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';

export class WatcherService {

    /**
     * @returns WatcherLMListResponse Success
     * @throws ApiError
     */
    public static getAllWatchers({
        userId,
        size,
        page,
        ascending,
        sortBy,
        searchTerm,
    }: {
        userId?: number,
        size?: number,
        page?: number,
        ascending?: boolean,
        sortBy?: string,
        searchTerm?: string,
    }): CancelablePromise<WatcherLMListResponse> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/watchers',
            query: {
                'userId': userId,
                'size': size,
                'page': page,
                'ascending': ascending,
                'sortBy': sortBy,
                'searchTerm': searchTerm,
            },
        });
    }

    /**
     * @returns WatcherVM Success
     * @throws ApiError
     */
    public static createWatcher({
        requestBody,
    }: {
        requestBody?: WatcherCM,
    }): CancelablePromise<WatcherVM> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/watchers',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Bad Request`,
            },
        });
    }

    /**
     * @returns WatcherVM Success
     * @throws ApiError
     */
    public static updateWatcher({
        watcherId,
        requestBody,
    }: {
        watcherId: number,
        requestBody?: WatcherUM,
    }): CancelablePromise<WatcherVM> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/watchers/{WatcherId}',
            path: {
                'watcherId': watcherId,
            },
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Bad Request`,
                404: `Not Found`,
            },
        });
    }

    /**
     * @returns void
     * @throws ApiError
     */
    public static deleteWatcher({
        watcherId,
    }: {
        watcherId: number,
    }): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/watchers/{WatcherId}',
            path: {
                'watcherId': watcherId,
            },
            errors: {
                404: `Not Found`,
            },
        });
    }

}
