// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import axios from './axiosJWTDecorator';
let baseGraphUrl = 'https://graph.microsoft.com/v1.0';

export async function getUserPhoto(userId: string): Promise<Blob> {

    let axiosConfig = {
        headers: {
            "Content-Type": "image/jpg"
        }
    };

    let url = `${baseGraphUrl}/users/${userId}/photos/48x48/$value`;

    return await axios.get(url, false, true, axiosConfig);
    //return response.blob();
    
}
