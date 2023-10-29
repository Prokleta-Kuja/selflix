/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { DeviceIdVM } from '../models/DeviceIdVM';
import type { UserDeviceCM } from '../models/UserDeviceCM';
import type { UserDeviceLMListResponse } from '../models/UserDeviceLMListResponse';
import type { UserDeviceUM } from '../models/UserDeviceUM';
import type { UserDeviceVM } from '../models/UserDeviceVM';

import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';

export class UserDeviceService {

    /**
     * @returns UserDeviceLMListResponse Success
     * @throws ApiError
     */
    public static getAllDevices({
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
    }): CancelablePromise<UserDeviceLMListResponse> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/devices',
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
     * @returns UserDeviceVM Success
     * @throws ApiError
     */
    public static createUserDevice({
        requestBody,
    }: {
        requestBody?: UserDeviceCM,
    }): CancelablePromise<UserDeviceVM> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/devices',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Bad Request`,
            },
        });
    }

    /**
     * @returns UserDeviceVM Success
     * @throws ApiError
     */
    public static updateUserDevice({
        userDeviceId,
        requestBody,
    }: {
        userDeviceId: number,
        requestBody?: UserDeviceUM,
    }): CancelablePromise<UserDeviceVM> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/devices/{userDeviceId}',
            path: {
                'userDeviceId': userDeviceId,
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
    public static deleteUserDevice({
        userDeviceId,
    }: {
        userDeviceId: number,
    }): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/devices/{userDeviceId}',
            path: {
                'userDeviceId': userDeviceId,
            },
            errors: {
                404: `Not Found`,
            },
        });
    }

    /**
     * @returns DeviceIdVM Success
     * @throws ApiError
     */
    public static generateDeviceId(): CancelablePromise<DeviceIdVM> {
        return __request(OpenAPI, {
            method: 'PATCH',
            url: '/api/devices/actions/generate-device-id',
        });
    }

    /**
     * @returns string Success
     * @throws ApiError
     */
    public static registerDevice({
        deviceId,
    }: {
        deviceId: string,
    }): CancelablePromise<string> {
        return __request(OpenAPI, {
            method: 'PATCH',
            url: '/api/devices/{deviceId}/actions/register',
            path: {
                'deviceId': deviceId,
            },
            errors: {
                404: `Not Found`,
            },
        });
    }

}
