// TypeScript bindings for emscripten-generated code.  Automatically generated at compile time.
declare namespace RuntimeExports {
    let HEAP_DATA_VIEW: any;
    let HEAP8: any;
    let HEAPU8: any;
    let HEAP16: any;
    let HEAPU16: any;
    let HEAP32: any;
    let HEAPU32: any;
    let HEAPF32: any;
    let HEAP64: any;
    let HEAPU64: any;
    let HEAPF64: any;
    function stackSave(): any;
    function stackRestore(val: any): any;
    function stackAlloc(sz: any): any;
    function lengthBytesUTF8(str: any): number;
    function stringToUTF8(str: any, outPtr: any, maxBytesToWrite: any): any;
    /**
     * Given a pointer 'ptr' to a null-terminated UTF8-encoded string in the
     * emscripten HEAP, returns a copy of that string as a Javascript String object.
     *
     * @param {number} ptr
     * @param {number=} maxBytesToRead - An optional length that specifies the
     *   maximum number of bytes to read. You can omit this parameter to scan the
     *   string until the first 0 byte. If maxBytesToRead is passed, and the string
     *   at [ptr, ptr+maxBytesToReadr[ contains a null byte in the middle, then the
     *   string will cut short at that byte index (i.e. maxBytesToRead will not
     *   produce a string of exact length [ptr, ptr+maxBytesToRead[) N.B. mixing
     *   frequent uses of UTF8ToString() with and without maxBytesToRead may throw
     *   JS JIT optimizations off, so it is worth to consider consistently using one
     * @return {string}
     */
    function UTF8ToString(ptr: number, maxBytesToRead?: number | undefined): string;
}
interface WasmModule {
  _string_getBytes(_0: number, _1: number, _2: number): void;
  _string_getByteCount(_0: number): number;
  _string_getLength(_0: number): number;
  _randomizableKeyframe_vector2_getIsRelative(_0: number): number;
  _randomizableKeyframe_vector2_getRandomInterval(_0: number): number;
  _randomizableKeyframe_vector2_getRandomValue(_0: number, _1: number): void;
  _randomizableKeyframe_vector2_getRandomMode(_0: number): number;
  _randomizableKeyframe_vector2_getValue(_0: number, _1: number): void;
  _randomizableKeyframe_vector2_new(_0: number, _1: number, _2: number, _3: number, _4: number, _5: number, _6: number): number;
  _randomizableKeyframe_float_getIsRelative(_0: number): number;
  _randomizableKeyframe_float_getRandomInterval(_0: number): number;
  _randomizableKeyframe_float_getRandomValue(_0: number): number;
  _randomizableKeyframe_float_getRandomMode(_0: number): number;
  _randomizableKeyframe_float_getValue(_0: number): number;
  _randomizableKeyframe_float_new(_0: number, _1: number, _2: number, _3: number, _4: number, _5: number, _6: number): number;
  _main_getAppPointer(): number;
  _main_shutdown(): void;
  _main_start(_0: BigInt, _1: number, _2: number, _3: number, _4: number): void;
  _keyframeList_iterator_dispose(_0: number): void;
  _keyframeList_iterator_getCurrent(_0: number): number;
  _keyframeList_iterator_reset(_0: number): void;
  _keyframeList_iterator_moveNext(_0: number): number;
  _keyframeList_getIterator(_0: number): number;
  _keyframeList_replace(_0: number, _1: number, _2: number): void;
  _keyframeList_removeAt(_0: number, _1: number): void;
  _keyframeList_add(_0: number, _1: number): void;
  _keyframeList_at(_0: number, _1: number): number;
  _keyframeList_getCount(_0: number): number;
  _keyframe_beatmapObjectIndexedColor_getValue(_0: number, _1: number): void;
  _keyframe_beatmapObjectIndexedColor_new(_0: number, _1: number, _2: number): number;
  _keyframe_getEase(_0: number): number;
  _keyframe_getTime(_0: number): number;
  _keyframe_getType(_0: number): number;
  _idContainer_iterator_dispose(_0: number): void;
  _idContainer_iterator_getCurrent_value(_0: number): number;
  _idContainer_iterator_getCurrent_key(_0: number): number;
  _idContainer_iterator_reset(_0: number): void;
  _idContainer_iterator_moveNext(_0: number): number;
  _idContainer_getIterator(_0: number): number;
  _idContainer_remove(_0: number, _1: number): number;
  _idContainer_insert(_0: number, _1: number): number;
  _idContainer_getById(_0: number, _1: number): number;
  _idContainer_getCount(_0: number): number;
  _interop_free(_0: number): void;
  _interop_alloc(_0: number): number;
  _interop_releasePointer(_0: number): void;
  _beatmapObject_getColorKeyframes(_0: number): number;
  _beatmapObject_getRotationKeyframes(_0: number): number;
  _beatmapObject_getScaleKeyframes(_0: number): number;
  _beatmapObject_getPositionKeyframes(_0: number): number;
  _beatmapObject_setText(_0: number, _1: number): void;
  _beatmapObject_getText(_0: number): number;
  _beatmapObject_setShape(_0: number, _1: number): void;
  _beatmapObject_getShape(_0: number): number;
  _beatmapObject_setAutoKillOffset(_0: number, _1: number): void;
  _beatmapObject_getAutoKillOffset(_0: number): number;
  _beatmapObject_setAutoKillType(_0: number, _1: number): void;
  _beatmapObject_getAutoKillType(_0: number): number;
  _beatmapObject_setStartTime(_0: number, _1: number): void;
  _beatmapObject_getStartTime(_0: number): number;
  _beatmapObject_setRenderDepth(_0: number, _1: number): void;
  _beatmapObject_getRenderDepth(_0: number): number;
  _beatmapObject_setOrigin(_0: number, _1: number): void;
  _beatmapObject_getOrigin(_0: number, _1: number): void;
  _beatmapObject_setRenderType(_0: number, _1: number): void;
  _beatmapObject_getRenderType(_0: number): number;
  _beatmapObject_setParentOffset(_0: number, _1: number): void;
  _beatmapObject_getParentOffset(_0: number, _1: number): void;
  _beatmapObject_setParentType(_0: number, _1: number): void;
  _beatmapObject_getParentType(_0: number): number;
  _beatmapObject_setType(_0: number, _1: number): void;
  _beatmapObject_getType(_0: number): number;
  _beatmapObject_setParentId(_0: number, _1: number): void;
  _beatmapObject_getParentId(_0: number): number;
  _beatmapObject_setName(_0: number, _1: number): void;
  _beatmapObject_getName(_0: number): number;
  _beatmapObject_getId(_0: number): number;
  _beatmapObject_new(_0: number): number;
  _beatmapData_getObjects(_0: number): number;
  _app_processFrame(_0: number, _1: number): void;
  _app_getBeatmapDataPointer(_0: number): number;
}

export type MainModule = WasmModule & typeof RuntimeExports;
export default function MainModuleFactory (options?: unknown): Promise<MainModule>;
