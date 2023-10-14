/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { SubDirVM } from './SubDirVM';
import type { VideoVM } from './VideoVM';

export type DirVM = {
    id: number;
    name: string;
    parentDirId?: number | null;
    subDirs?: Array<SubDirVM>;
    videos?: Array<VideoVM>;
};

