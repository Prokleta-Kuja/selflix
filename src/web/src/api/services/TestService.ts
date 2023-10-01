/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';

export class TestService {

    /**
     * @returns any Success
     * @throws ApiError
     */
    public static test(): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/Test',
        });
    }

    /**
     * @returns any Success
     * @throws ApiError
     */
    public static testHead(): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'HEAD',
            url: '/Test',
        });
    }

}
