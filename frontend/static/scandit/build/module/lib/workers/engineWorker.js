/**
 * @hidden
 *
 * @returns Engine
 */
// tslint:disable-next-line:max-func-body-length
export function engine() {
    const writableDataPathPreload = "/scandit_sync_folder_preload";
    const writableDataPathStandard = "/scandit_sync_folder";
    const scanQueue = [];
    const parseQueue = [];
    const gpuAccelerationAvailable = typeof self.OffscreenCanvas === "function";
    let originalFSSyncfs;
    let imageBufferPointer;
    let imageBufferSize;
    let preloading;
    let writableDataPath;
    let delayedRegistration;
    let licenseKey;
    let scanSettings;
    let imageSettings;
    let cameraProperties;
    let blurryRecognitionAvailable = false;
    let workSubmitted = false;
    let loadingInProgress = false;
    let fileSystemSynced = false;
    let runtimeLoaded = false;
    let wasmReady = false;
    let scannerSettingsReady = false;
    let scannerImageSettingsReady = false;
    let contextAvailable = false;
    let fsSyncPromise = Promise.resolve();
    let fsSyncInProgress = false;
    let fsSyncScheduled = false;
    // Public
    // Promise is used only during testing
    function loadLibrary(deviceId, libraryLocation, locationPath, preload, newDelayedRegistration, newLicenseKey, deviceModelName) {
        function reportLoadSuccess() {
            postMessage(["library-loaded"]);
            createContext(newDelayedRegistration, newLicenseKey);
        }
        function start() {
            if (!wasmReady && fileSystemSynced && runtimeLoaded) {
                loadingInProgress = false;
                Module.callMain();
                wasmReady = true;
                reportLoadSuccess();
                if (!newDelayedRegistration) {
                    workOnScanQueue();
                    workOnParseQueue();
                }
            }
        }
        if (loadingInProgress) {
            return Promise.resolve();
        }
        if (wasmReady) {
            reportLoadSuccess();
            return Promise.resolve();
        }
        loadingInProgress = true;
        const { jsURI, wasmURI } = getLibraryLocationURIs(libraryLocation);
        preloading = preload;
        writableDataPath = preload ? writableDataPathPreload : writableDataPathStandard;
        self.window = self.document = self; // Fix some Emscripten quirks
        self.path = locationPath; // Used by the Scandit SDK Engine library
        self.deviceModelName = deviceModelName; // Used by the Scandit SDK Engine library
        Module = {
            arguments: [deviceId],
            canvas: gpuAccelerationAvailable ? new self.OffscreenCanvas(32, 32) : /* istanbul ignore next */ undefined,
            instantiateWasm: (importObject, successCallback) => {
                // wasmJSVersion is globally defined inside scandit-engine-sdk.min.js
                const wasmJSVersion = self.wasmJSVersion ?? "undefined";
                // istanbul ignore if
                if (wasmJSVersion !== "5.5.3") {
                    console.error(`The Scandit SDK Engine library JS file found at ${jsURI} seems invalid: ` +
                        `expected version doesn't match (received: ${wasmJSVersion}, expected: ${"5.5.3"}). ` +
                        `Please ensure the correct Scandit SDK Engine file (with correct version) is retrieved.`);
                }
                if (typeof self.WebAssembly.instantiateStreaming === "function") {
                    instantiateWebAssemblyStreaming(importObject, wasmURI, successCallback);
                }
                else {
                    instantiateWebAssembly(importObject, wasmURI, successCallback);
                }
                return {};
            },
            noInitialRun: true,
            preRun: [
                () => {
                    return setupFS()
                        .catch((error) => {
                        console.debug("No IndexedDB support, some data will not be persisted:", error);
                    })
                        .then(() => {
                        fileSystemSynced = true;
                        start();
                    });
                },
            ],
            onRuntimeInitialized: () => {
                runtimeLoaded = true;
                start();
            },
        };
        function tryImportScripts() {
            try {
                return importScripts(jsURI) ?? Promise.resolve();
            }
            catch (error) {
                return Promise.reject(error);
            }
        }
        return retryWithExponentialBackoff(tryImportScripts, 250, 4000, (error) => {
            console.warn(error);
            console.warn(`Couldn't retrieve Scandit SDK Engine library at ${jsURI}, retrying...`);
        }).catch((error) => {
            console.error(error);
            console.error(`Couldn't retrieve Scandit SDK Engine library at ${jsURI}, did you configure the path for it correctly?`);
            return Promise.resolve(error); // Promise is used only during testing
        });
    }
    // tslint:disable-next-line: bool-param-default
    function createContext(newDelayedRegistration, newLicenseKey) {
        function completeCreateContext() {
            postMessage([
                "context-created",
                {
                    hiddenScanditLogoAllowed: Module._can_hide_logo() === 1,
                },
            ]);
        }
        if (contextAvailable) {
            return completeCreateContext();
        }
        if (newDelayedRegistration != null) {
            delayedRegistration = newDelayedRegistration;
        }
        if (newLicenseKey != null) {
            licenseKey = newLicenseKey;
        }
        if (!wasmReady || delayedRegistration == null || (!workSubmitted && !delayedRegistration) || licenseKey == null) {
            return;
        }
        const licenseKeyLength = lengthBytesUTF8(licenseKey) + 1;
        const licenseKeyPointer = Module._malloc(licenseKeyLength);
        stringToUTF8(licenseKey, licenseKeyPointer, licenseKeyLength);
        const writableDataPathLength = lengthBytesUTF8(writableDataPath) + 1;
        const writableDataPathPointer = Module._malloc(writableDataPathLength);
        stringToUTF8(writableDataPath, writableDataPathPointer, writableDataPathLength);
        Module._create_context(licenseKeyPointer, writableDataPathPointer, delayedRegistration, false);
        Module._free(licenseKeyPointer);
        Module._free(writableDataPathPointer);
        contextAvailable = true;
        reportCameraProperties();
        completeCreateContext();
    }
    function setScanSettings(newScanSettings, newBlurryRecognitionAvailable, blurryRecognitionRequiresUpdate) {
        function completeSetScanSettings() {
            scanSettings = newScanSettings;
            blurryRecognitionAvailable = newBlurryRecognitionAvailable;
            applyScanSettings();
            workOnScanQueue();
        }
        scanSettings = undefined;
        scannerSettingsReady = false;
        if (newBlurryRecognitionAvailable && blurryRecognitionRequiresUpdate) {
            syncFS(true, false, true).then(completeSetScanSettings).catch(completeSetScanSettings);
        }
        else {
            completeSetScanSettings();
        }
    }
    function setImageSettings(newImageSettings) {
        imageSettings = newImageSettings;
        applyImageSettings();
        workOnScanQueue();
    }
    function augmentErrorInformation(error) {
        if (error.errorCode === 260) {
            let hostname;
            // istanbul ignore if
            if (location.href?.indexOf("blob:null/") === 0) {
                hostname = "localhost";
            }
            else {
                hostname = new URL(location.pathname != null && location.pathname !== "" && !location.pathname.startsWith("/")
                    ? /* istanbul ignore next */ location.pathname
                    : location.origin).hostname;
            }
            // istanbul ignore next
            if (hostname[0].startsWith("[") && hostname.endsWith("]")) {
                hostname = hostname.slice(1, -1);
            }
            error.errorMessage = error.errorMessage.replace("domain name", `domain name (${hostname})`);
        }
        // License Key related error codes from 6 to 25 and 260
        if ([6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 260].includes(error.errorCode) &&
            licenseKey != null &&
            licenseKey.length > 0) {
            error.errorMessage += ` License Key: ${licenseKey.slice(0, 15)}...`;
        }
    }
    function processScanWorkUnit(scanWorkUnit) {
        if (scanWorkUnit.highQualitySingleFrameMode) {
            applyScanSettings(true);
        }
        const resultData = scanImage(scanWorkUnit.data);
        if (scanWorkUnit.highQualitySingleFrameMode) {
            applyScanSettings(false);
        }
        const result = JSON.parse(resultData);
        if (result.error != null) {
            augmentErrorInformation(result.error);
            postMessage([
                "work-error",
                {
                    requestId: scanWorkUnit.requestId,
                    error: result.error,
                },
                scanWorkUnit.data,
            ], [scanWorkUnit.data.buffer]);
        }
        else {
            // istanbul ignore else
            if (result.scanResult != null) {
                postMessage([
                    "work-result",
                    {
                        requestId: scanWorkUnit.requestId,
                        result,
                    },
                    scanWorkUnit.data,
                ], [scanWorkUnit.data.buffer]);
            }
        }
    }
    function workOnScanQueue() {
        if (!wasmReady || scanQueue.length === 0) {
            return;
        }
        // Initialization for first submitted work unit
        if (!contextAvailable) {
            createContext();
        }
        if (!scannerSettingsReady) {
            applyScanSettings();
        }
        if (!scannerImageSettingsReady) {
            applyImageSettings();
        }
        if (!contextAvailable || !scannerSettingsReady || !scannerImageSettingsReady) {
            return;
        }
        while (scanQueue.length !== 0) {
            if (scanQueue[0].highQualitySingleFrameMode && !blurryRecognitionAvailable) {
                break;
            }
            processScanWorkUnit(scanQueue.shift());
        }
    }
    function processParseWorkUnit(parseWorkUnit) {
        const resultData = parse(parseWorkUnit.dataFormat, parseWorkUnit.data, parseWorkUnit.options);
        const result = JSON.parse(resultData);
        if (result.error != null) {
            augmentErrorInformation(result.error);
            postMessage([
                "parse-error",
                {
                    requestId: parseWorkUnit.requestId,
                    error: result.error,
                },
            ]);
        }
        else {
            // istanbul ignore else
            if (result.result != null) {
                postMessage([
                    "parse-result",
                    {
                        requestId: parseWorkUnit.requestId,
                        result: result.result,
                    },
                ]);
            }
        }
    }
    function workOnParseQueue() {
        if (!wasmReady || parseQueue.length === 0) {
            return;
        }
        // Initialization for first submitted work unit
        if (!contextAvailable) {
            createContext();
            if (!contextAvailable) {
                return;
            }
        }
        while (parseQueue.length !== 0) {
            processParseWorkUnit(parseQueue.shift());
        }
    }
    function addScanWorkUnit(scanWorkUnit) {
        workSubmitted = true;
        scanQueue.push(scanWorkUnit);
        workOnScanQueue();
    }
    function addParseWorkUnit(parseWorkUnit) {
        workSubmitted = true;
        parseQueue.push(parseWorkUnit);
        workOnParseQueue();
    }
    function clearSession() {
        if (scannerSettingsReady) {
            Module._scanner_session_clear();
        }
    }
    function createBlurryTable(symbology) {
        function reportResult() {
            postMessage(["create-blurry-table-result", symbology]);
        }
        if (!wasmReady || !contextAvailable) {
            return;
        }
        const symbologyLength = lengthBytesUTF8(symbology) + 1;
        const symbologyPointer = Module._malloc(symbologyLength);
        stringToUTF8(symbology, symbologyPointer, symbologyLength);
        Module._create_blurry_table(symbologyPointer);
        Module._free(symbologyPointer);
        // FS.syncfs is called by the engine if needed: the current promise is the one to wait for
        fsSyncPromise.then(reportResult).catch(
        // istanbul ignore next
        reportResult);
    }
    function setCameraProperties(cameraType, autofocus) {
        cameraProperties = {
            cameraType,
            autofocus,
        };
        if (!wasmReady || !contextAvailable) {
            return;
        }
        reportCameraProperties();
    }
    function reset() {
        clearSession();
        scanQueue.length = 0;
        parseQueue.length = 0;
        scanSettings = undefined;
        imageSettings = undefined;
        workSubmitted = false;
        scannerSettingsReady = false;
        scannerImageSettingsReady = false;
    }
    // Private
    function retryWithExponentialBackoff(handler, backoffMs, maxBackoffMs, singleTryRejectionCallback) {
        return new Promise((resolve, reject) => {
            handler()
                .then(resolve)
                .catch((error) => {
                const newBackoffMs = backoffMs * 2;
                if (newBackoffMs > maxBackoffMs) {
                    return reject(error);
                }
                singleTryRejectionCallback(error);
                setTimeout(() => {
                    retryWithExponentialBackoff(handler, newBackoffMs, maxBackoffMs, singleTryRejectionCallback)
                        .then(resolve)
                        .catch(reject);
                }, backoffMs);
            });
        });
    }
    function getLibraryLocationURIs(libraryLocation) {
        let cdnURI = false;
        if (/^https?:\/\/([^\/.]*\.)*cdn.jsdelivr.net\//.test(libraryLocation)) {
            libraryLocation = "https://cdn.jsdelivr.net/npm/scandit-sdk@5.5.3/build/";
            cdnURI = true;
        }
        else if (/^https?:\/\/([^\/.]*\.)*unpkg.com\//.test(libraryLocation)) {
            libraryLocation = "https://unpkg.com/scandit-sdk@5.5.3/build/";
            cdnURI = true;
        }
        if (cdnURI) {
            return {
                jsURI: `${libraryLocation}scandit-engine-sdk.min.js`,
                wasmURI: `${libraryLocation}scandit-engine-sdk.wasm`,
            };
        }
        return {
            jsURI: `${libraryLocation}scandit-engine-sdk.min.js?v=5.5.3`,
            wasmURI: `${libraryLocation}scandit-engine-sdk.wasm?v=5.5.3`,
        };
    }
    function arrayBufferToHexString(arrayBuffer) {
        return Array.from(new Uint8Array(arrayBuffer))
            .map((byteNumber) => {
            const byteHex = byteNumber.toString(16);
            return byteHex.length === 1 ? /* istanbul ignore next */ `0${byteHex}` : byteHex;
        })
            .join("");
    }
    function applyScanSettings(highQualitySingleFrameMode = false) {
        if (!wasmReady || !contextAvailable || !workSubmitted || scanSettings == null) {
            return;
        }
        scannerSettingsReady = false;
        const parsedSettings = JSON.parse(scanSettings);
        const scanSettingsLength = lengthBytesUTF8(scanSettings) + 1;
        const scanSettingsPointer = Module._malloc(scanSettingsLength);
        stringToUTF8(scanSettings, scanSettingsPointer, scanSettingsLength);
        const resultPointer = Module._scanner_settings_new_from_json(scanSettingsPointer, parsedSettings.blurryRecognition && blurryRecognitionAvailable, parsedSettings.matrixScanEnabled, highQualitySingleFrameMode, gpuAccelerationAvailable && parsedSettings.gpuAcceleration);
        Module._free(scanSettingsPointer);
        const result = UTF8ToString(resultPointer);
        if (result !== "") {
            scannerSettingsReady = true;
            console.debug("External Scandit Engine scan settings:", JSON.parse(result));
        }
    }
    function applyImageSettings() {
        if (!wasmReady || !contextAvailable || !workSubmitted || imageSettings == null) {
            return;
        }
        scannerImageSettingsReady = false;
        let channels;
        // TODO: For now it's not possible to use imported variables as the worker doesn't have access at runtime
        if (imageSettings.format.valueOf() === 1) {
            // RGB_8U
            channels = 3;
        }
        else if (imageSettings.format.valueOf() === 2) {
            // RGBA_8U
            channels = 4;
        }
        else {
            // GRAY_8U
            channels = 1;
        }
        Module._scanner_image_settings_new(imageSettings.width, imageSettings.height, channels);
        if (imageBufferPointer != null) {
            Module._free(imageBufferPointer);
        }
        imageBufferSize = imageSettings.width * imageSettings.height * channels;
        imageBufferPointer = Module._malloc(imageBufferSize);
        scannerImageSettingsReady = true;
    }
    function reportCameraProperties() {
        if (!wasmReady || !contextAvailable || cameraProperties == null) {
            return;
        }
        // TODO: For now it's not possible to use imported variables as the worker doesn't have access at runtime
        Module._report_camera_properties(cameraProperties.cameraType === "front", cameraProperties.autofocus);
    }
    function scanImage(imageData) {
        if (imageData.byteLength !== imageBufferSize) {
            // This could happen in unexpected situations and should be temporary
            return JSON.stringify({ scanResult: [] });
        }
        Module.HEAPU8.set(imageData, imageBufferPointer);
        return UTF8ToString(Module._scanner_scan(imageBufferPointer));
    }
    function parse(dataFormat, data, options) {
        const dataLength = typeof data === "string" ? lengthBytesUTF8(data) + 1 : data.byteLength;
        const dataPointer = Module._malloc(dataLength);
        if (typeof data === "string") {
            stringToUTF8(data, dataPointer, dataLength);
        }
        else {
            Module.HEAPU8.set(data, dataPointer);
        }
        const optionsLength = lengthBytesUTF8(options) + 1;
        const optionsPointer = Module._malloc(optionsLength);
        stringToUTF8(options, optionsPointer, optionsLength);
        const resultPointer = Module._parser_parse_string(dataFormat.valueOf(), dataPointer, dataLength - 1, optionsPointer);
        Module._free(dataPointer);
        Module._free(optionsPointer);
        return UTF8ToString(resultPointer);
    }
    function verifiedWasmFetch(wasmURI, awaitFullResponse) {
        function verifyResponseData(responseData) {
            // istanbul ignore else
            if (typeof crypto?.subtle?.digest === "function") {
                crypto.subtle
                    .digest("SHA-256", responseData)
                    .then((hash) => {
                    const hashString = arrayBufferToHexString(hash);
                    // istanbul ignore if
                    if (hashString !== "967ea4904a0e287339e38a58294da18ecd55c306268167c610a2ddba2a79a943") {
                        console.error(`The Scandit SDK Engine library WASM file found at ${wasmURI} seems invalid: ` +
                            `expected file hash doesn't match (received: ${hashString}, ` +
                            `expected: ${"967ea4904a0e287339e38a58294da18ecd55c306268167c610a2ddba2a79a943"}). ` +
                            `Please ensure the correct Scandit SDK Engine file (with correct version) is retrieved.`);
                    }
                })
                    .catch(
                /* istanbul ignore next */ () => {
                    // Ignored
                });
            }
            else {
                console.warn("Insecure context (see https://developer.mozilla.org/en-US/docs/Web/Security/Secure_Contexts): " +
                    `The hash of the Scandit SDK Engine library WASM file found at ${wasmURI} could not be verified`);
            }
        }
        function tryFetch() {
            return new Promise((resolve, reject) => {
                fetch(wasmURI)
                    .then((response) => {
                    // istanbul ignore else
                    if (response.ok) {
                        response
                            .clone()
                            .arrayBuffer()
                            .then((responseData) => {
                            if (awaitFullResponse) {
                                resolve(response);
                            }
                            verifyResponseData(responseData);
                        })
                            .catch(
                        // istanbul ignore next
                        (error) => {
                            if (awaitFullResponse) {
                                reject(error);
                            }
                        });
                        if (!awaitFullResponse) {
                            resolve(response);
                        }
                    }
                    else {
                        reject(new Error("HTTP status code is not ok"));
                    }
                })
                    .catch((error) => {
                    reject(error);
                });
            });
        }
        return retryWithExponentialBackoff(tryFetch, 250, 4000, (error) => {
            console.warn(error);
            console.warn(`Couldn't retrieve Scandit SDK Engine library at ${wasmURI}, retrying...`);
        }).catch((error) => {
            console.error(error);
            console.error(`Couldn't retrieve/instantiate Scandit SDK Engine library at ${wasmURI}, ` +
                "did you configure the path for it correctly?");
            return Promise.reject(error);
        });
    }
    function instantiateWebAssembly(importObject, wasmURI, successCallback) {
        verifiedWasmFetch(wasmURI, true)
            .then((response) => {
            return response.arrayBuffer();
        })
            .then((bytes) => {
            return self.WebAssembly.instantiate(bytes, importObject)
                .then((results) => {
                successCallback(results.instance);
            })
                .catch((error) => {
                console.error(error);
                console.error(`Couldn't instantiate Scandit SDK Engine library at ${wasmURI}, ` +
                    "did you configure the path for it correctly?");
            });
        })
            .catch(
        /* istanbul ignore next */ () => {
            // Ignored
        });
    }
    function instantiateWebAssemblyStreaming(importObject, wasmURI, successCallback) {
        verifiedWasmFetch(wasmURI, false)
            .then((response) => {
            self.WebAssembly.instantiateStreaming(response, importObject)
                .then((results) => {
                successCallback(results.instance);
            })
                .catch((error) => {
                console.warn(error);
                console.warn("WebAssembly streaming compile failed. " +
                    "Falling back to ArrayBuffer instantiation (this will make things slower)");
                instantiateWebAssembly(importObject, wasmURI, successCallback);
            });
        })
            .catch(
        /* istanbul ignore next */ () => {
            // Ignored
        });
    }
    function syncFSMergePreloadedData() {
        const fsObjectStoreName = "FILE_DATA";
        let resolveCallback;
        let openDbSourceRequest;
        let openDbTargetRequest;
        function handleError() {
            openDbSourceRequest?.result?.close();
            openDbTargetRequest?.result?.close();
            // this.error
            resolveCallback(0);
        }
        function performMerge() {
            try {
                const objects = [];
                const sourceTransaction = openDbSourceRequest.result.transaction(fsObjectStoreName, "readonly");
                sourceTransaction.onerror = handleError;
                const cursorRequest = sourceTransaction
                    .objectStore(fsObjectStoreName)
                    .openCursor();
                cursorRequest.onsuccess = () => {
                    const cursor = cursorRequest.result;
                    if (cursor == null) {
                        try {
                            let mergedObjectsCount = 0;
                            const targetTransaction = openDbTargetRequest.result.transaction(fsObjectStoreName, "readwrite");
                            const targetObjectStore = targetTransaction.objectStore(fsObjectStoreName);
                            targetTransaction.onerror = handleError;
                            targetTransaction.oncomplete = () => {
                                openDbSourceRequest.result.close();
                                openDbTargetRequest.result.close();
                                return resolveCallback(mergedObjectsCount);
                            };
                            for (const object of objects) {
                                const countRequest = targetObjectStore.count(object.primaryKey);
                                countRequest.onsuccess = () => {
                                    if (countRequest.result === 0) {
                                        ++mergedObjectsCount;
                                        targetObjectStore.add(object.value, object.primaryKey);
                                    }
                                };
                            }
                        }
                        catch (error) {
                            // istanbul ignore next
                            handleError.call({ error });
                        }
                    }
                    else {
                        objects.push({
                            value: cursor.value,
                            primaryKey: cursor.primaryKey
                                .toString()
                                .replace(`${writableDataPathPreload}/`, `${writableDataPathStandard}/`),
                        });
                        cursor.continue();
                    }
                };
                cursorRequest.onerror = handleError;
            }
            catch (error) {
                // istanbul ignore next
                handleError.call({ error });
            }
        }
        return new Promise((resolve) => {
            resolveCallback = resolve;
            openDbSourceRequest = indexedDB.open(writableDataPathPreload);
            openDbSourceRequest.onupgradeneeded = () => {
                try {
                    openDbSourceRequest.result.createObjectStore(fsObjectStoreName);
                }
                catch (error) {
                    // Ignored
                }
            };
            openDbSourceRequest.onsuccess = () => {
                if (!Array.from(openDbSourceRequest.result.objectStoreNames).includes(fsObjectStoreName)) {
                    return resolve(0);
                }
                openDbTargetRequest = indexedDB.open(writableDataPathStandard);
                openDbTargetRequest.onupgradeneeded = () => {
                    try {
                        openDbTargetRequest.result.createObjectStore(fsObjectStoreName);
                    }
                    catch (error) {
                        // Ignored
                    }
                };
                openDbTargetRequest.onsuccess = () => {
                    performMerge();
                };
                openDbTargetRequest.onblocked = openDbTargetRequest.onerror = handleError;
            };
            openDbSourceRequest.onblocked = openDbSourceRequest.onerror = handleError;
        });
    }
    function syncFSPromisified(populate, initialPopulation) {
        // istanbul ignore if
        if (originalFSSyncfs == null) {
            return Promise.resolve();
        }
        fsSyncInProgress = true;
        return new Promise((resolve, reject) => {
            // Merge with data coming from preloading workers if needed
            (!preloading && populate ? syncFSMergePreloadedData() : Promise.resolve(0))
                .then((mergedObjects) => {
                if (!preloading && populate && !initialPopulation && mergedObjects === 0) {
                    fsSyncInProgress = false;
                    return resolve();
                }
                // tslint:disable-next-line: no-non-null-assertion
                originalFSSyncfs(populate, (error) => {
                    fsSyncInProgress = false;
                    // istanbul ignore if
                    if (error != null) {
                        return reject(error);
                    }
                    resolve();
                });
            })
                .catch(reject);
        });
    }
    function syncFS(populate, initialPopulation = false, forceScheduling = false) {
        if (!fsSyncScheduled || forceScheduling) {
            if (fsSyncInProgress) {
                fsSyncScheduled = true;
                fsSyncPromise = fsSyncPromise.then(() => {
                    fsSyncScheduled = false;
                    return syncFSPromisified(populate, initialPopulation);
                });
            }
            else {
                fsSyncPromise = syncFSPromisified(populate, initialPopulation);
            }
        }
        return fsSyncPromise;
    }
    function setupFS() {
        // FS.syncfs is also called by the engine on file storage, ensure everything is coordinated nicely
        originalFSSyncfs = FS.syncfs;
        FS.syncfs = ((populate, callback) => {
            const originalCallback = callback;
            callback = (error) => {
                originalCallback(error);
            };
            syncFS(populate).then(callback).catch(callback);
        });
        try {
            FS.mkdir(writableDataPath);
        }
        catch (error) {
            // istanbul ignore next
            if (error.code !== "EEXIST") {
                originalFSSyncfs = undefined;
                return Promise.reject(error);
            }
        }
        FS.mount(IDBFS, {}, writableDataPath);
        return syncFS(true, true);
    }
    return {
        loadLibrary,
        setScanSettings,
        setImageSettings,
        workOnScanQueue,
        workOnParseQueue,
        addScanWorkUnit,
        addParseWorkUnit,
        clearSession,
        createBlurryTable,
        setCameraProperties,
        reset,
    };
}
// istanbul ignore next
function engineWorkerFunction() {
    const engineInstance = engine();
    onmessage = (e) => {
        // Creating context triggers license key verification and activation: delay until first frame processed
        const data = e.data;
        switch (data.type) {
            case "load-library":
                // tslint:disable-next-line: no-floating-promises
                engineInstance.loadLibrary(data.deviceId, data.libraryLocation, data.path, data.preload, data.delayedRegistration, data.licenseKey, data.deviceModelName);
                break;
            case "scan-settings":
                engineInstance.setScanSettings(data.settings, data.blurryRecognitionAvailable, data.blurryRecognitionRequiresUpdate);
                break;
            case "image-settings":
                engineInstance.setImageSettings(data.imageSettings);
                break;
            case "scan-image":
                engineInstance.addScanWorkUnit({
                    requestId: data.requestId,
                    data: data.data,
                    highQualitySingleFrameMode: data.highQualitySingleFrameMode,
                });
                break;
            case "parse":
                engineInstance.addParseWorkUnit({
                    requestId: data.requestId,
                    dataFormat: data.dataFormat,
                    data: data.data,
                    options: data.options,
                });
                break;
            case "clear-session":
                engineInstance.clearSession();
                break;
            case "create-blurry-table":
                engineInstance.createBlurryTable(data.symbology);
                break;
            case "camera-properties":
                engineInstance.setCameraProperties(data.cameraType, data.autofocus);
                break;
            case "reset":
                engineInstance.reset();
                break;
            default:
                break;
        }
    };
}
/**
 * @hidden
 */
export const engineWorkerBlob = new Blob([`var Module;${engine.toString()}(${engineWorkerFunction.toString()})()`], {
    type: "text/javascript",
});
//# sourceMappingURL=engineWorker.js.map