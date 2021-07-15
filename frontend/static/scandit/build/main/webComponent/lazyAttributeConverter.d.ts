import { Barcode, BarcodePicker, Camera, CameraSettings, ScanSettings, SearchArea, SingleImageModeSettings } from "..";
import { BarcodePickerView } from "./barcodePickerView";
import { Attribute, AttributeDescriptor } from "./schema";
export declare class LazyAttributeConverter {
    private readonly schema;
    private readonly view;
    constructor(schema: {
        [key in Attribute]: AttributeDescriptor;
    }, view: BarcodePickerView);
    get [Attribute.ACCESS_CAMERA](): boolean;
    get [Attribute.CAMERA](): Camera;
    get [Attribute.CAMERA_SETTINGS](): CameraSettings;
    get [Attribute.CAMERA_TYPE](): Camera.Type;
    get [Attribute.CONFIGURE](): boolean;
    get [Attribute.ENABLE_CAMERA_SWITCHER](): boolean;
    get [Attribute.ENABLE_PINCH_TO_ZOOM](): boolean;
    get [Attribute.ENABLE_TAP_TO_FOCUS](): boolean;
    get [Attribute.ENABLE_TORCH_TOGGLE](): boolean;
    get [Attribute.GUI_STYLE](): BarcodePicker.GuiStyle;
    get [Attribute.LASER_AREA](): SearchArea;
    get [Attribute.PLAY_SOUND_ON_SCAN](): boolean;
    get [Attribute.SCANNING_PAUSED](): boolean;
    get [Attribute.SINGLE_IMAGE_MODE_SETTINGS](): SingleImageModeSettings;
    get [Attribute.TARGET_SCANNING_FPS](): number;
    get [Attribute.VIBRATE_ON_SCAN](): boolean;
    get [Attribute.VIDEO_FIT](): BarcodePicker.ObjectFit;
    get [Attribute.VIEWFINDER_AREA](): SearchArea;
    get [Attribute.VISIBLE](): boolean;
    get [Attribute.CONFIGURE_ENGINE_LOCATION](): string;
    get [Attribute.CONFIGURE_LICENSE_KEY](): string;
    get [Attribute.CONFIGURE_PRELOAD_ENGINE](): boolean;
    get [Attribute.CONFIGURE_PRELOAD_BLURRY_RECOGNITION](): boolean;
    get [Attribute.SCAN_SETTINGS_BLURRY_RECOGNITION](): boolean;
    get [Attribute.SCAN_SETTINGS_CODE_DIRECTION_HINT](): ScanSettings.CodeDirection;
    get [Attribute.SCAN_SETTINGS_CODE_DUPLICATE_FILTER](): number;
    get [Attribute.SCAN_SETTINGS_ENABLED_SYMBOLOGIES](): Barcode.Symbology[];
    get [Attribute.SCAN_SETTINGS_GPU_ACCELERATION](): boolean;
    get [Attribute.SCAN_SETTINGS_MAX_NUMBER_OF_CODES_PER_FRAME](): number;
    get [Attribute.SCAN_SETTINGS_SEARCH_AREA](): SearchArea;
    private convertToPrimary;
}
