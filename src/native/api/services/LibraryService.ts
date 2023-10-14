/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { DirVM } from '../models/DirVM';
import type { LibraryCM } from '../models/LibraryCM';
import type { LibraryLMListResponse } from '../models/LibraryLMListResponse';
import type { LibraryUM } from '../models/LibraryUM';
import type { LibraryVM } from '../models/LibraryVM';

import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';

export class LibraryService {

    /**
     * @returns LibraryLMListResponse Success
     * @throws ApiError
     */
    public static getAllLibraries({
        size,
        page,
        ascending,
        sortBy,
        searchTerm,
    }: {
        size?: number,
        page?: number,
        ascending?: boolean,
        sortBy?: string,
        searchTerm?: string,
    }): CancelablePromise<LibraryLMListResponse> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/libraries',
            query: {
                'size': size,
                'page': page,
                'ascending': ascending,
                'sortBy': sortBy,
                'searchTerm': searchTerm,
            },
        });
    }

    /**
     * @returns LibraryVM Success
     * @throws ApiError
     */
    public static createLibrary({
        requestBody,
    }: {
        requestBody?: LibraryCM,
    }): CancelablePromise<LibraryVM> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/libraries',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Bad Request`,
            },
        });
    }

    /**
     * @returns LibraryVM Success
     * @throws ApiError
     */
    public static updateLibrary({
        libraryId,
        requestBody,
    }: {
        libraryId: number,
        requestBody?: LibraryUM,
    }): CancelablePromise<LibraryVM> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/libraries/{LibraryId}',
            path: {
                'libraryId': libraryId,
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
    public static deleteLibrary({
        libraryId,
    }: {
        libraryId: number,
    }): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/libraries/{libraryId}',
            path: {
                'libraryId': libraryId,
            },
            errors: {
                404: `Not Found`,
            },
        });
    }

    /**
     * @returns void
     * @throws ApiError
     */
    public static scheduleIndex({
        libraryId,
        fullIndex,
    }: {
        libraryId: number,
        fullIndex: boolean,
    }): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'PATCH',
            url: '/api/libraries/{libraryId}/actions/schedule-index/{fullIndex}',
            path: {
                'libraryId': libraryId,
                'fullIndex': fullIndex,
            },
            errors: {
                404: `Not Found`,
            },
        });
    }

    /**
     * @returns string Success
     * @throws ApiError
     */
    public static getAllPaths(): CancelablePromise<Array<string>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/libraries/paths',
        });
    }

    /**
     * @returns DirVM Success
     * @throws ApiError
     */
    public static getDirectory({
        libraryId,
        directoryId,
    }: {
        libraryId: number,
        directoryId: number,
    }): CancelablePromise<DirVM> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/libraries/{libraryId}/dirs/{directoryId}',
            path: {
                'libraryId': libraryId,
                'directoryId': directoryId,
            },
            errors: {
                404: `Not Found`,
            },
        });
    }

}
