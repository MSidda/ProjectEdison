// This file can be replaced during build by using the `fileReplacements` array.
// `ng build ---prod` replaces `environment.ts` with `environment.prod.ts`.
// The list of file replacements can be found in `angular.json`.

export const environment = {
    production: false,
    mockData: false,
    authorize: true,
    mapDefaults: {
        zoom: 16,
        style: {
            elements: {
                road: {
                    strokeColor: "#5b5b5b",
                    fillColor: "#ffffff"
                },
                controlledAccessHighway: {
                    strokeColor: "#5b5b5b",
                    fillColor: "#5b5b5b",
                },
                highway: {
                    strokeColor: "#5b5b5b",
                    fillColor: "#5b5b5b",
                },
                tollRoad: {
                    strokeColor: "#5b5b5b",
                    fillColor: "#5b5b5b",
                },
                education: {
                    fillColor: "#ffffff",
                }
            },
            settings: {
                landColor: "#E4E4E4",
            },
            version: "1.0"
        }
    },
    azureAd: {
        clientId: '2373be1e-6d0b-4e38-9115-e0bd01dadd61',
        authority: 'https://login.microsoftonline.com/',
        tenant: '1114b48d-24b1-4492-970a-d07d610a741c'
    },
    chat: {
        secret: '' /* put your Direct Line secret here */,
        // token: , /* or put your Direct Line token here (supply secret OR token, not both) */,
        // domain: /* optional: if you are not using the default Direct Line endpoint, e.g. if you are using a region-specific endpoint, put its full URL here */
        // webSocket: /* optional: false if you want to use polling GET to receive messages. Defaults to true (use WebSocket). */,
        // pollingInterval: /* optional: set polling interval in milliseconds. Default to 1000 */,
    },
    apiUrl: '/api/',
    baseUrl: 'https://edisonapidev.eastus.cloudapp.azure.com',
    signalRUrl: '/signalr/',
    chatAuthUrl: '/chat/security/gettoken/',
    bingMapsKey:
        'Akt7a75JIqQ-QV2ZzHVP76eivabKNvlcq_JtF8zeTePsI38tt0LdAtAFeyh1MBrz'
}

/*
 * In development mode, for easier debugging, you can ignore zone related error
 * stack frames such as `zone.run`/`zoneDelegate.invokeTask` by importing the
 * below file. Don't forget to comment it out in production mode
 * because it will have a performance impact when errors are thrown
 */
// import 'zone.js/dist/zone-error';  // Included with Angular CLI.
