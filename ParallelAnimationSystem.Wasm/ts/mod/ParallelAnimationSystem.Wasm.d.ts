// TypeScript bindings for emscripten-generated code.  Automatically generated at compile time.
declare namespace RuntimeExports {
    let HEAP8: any;
    let HEAPU8: any;
    let HEAP16: any;
    let HEAPU16: any;
    let HEAP32: any;
    let HEAPU32: any;
    let HEAPF32: any;
    let HEAPF64: any;
    function stringToNewUTF8(str: any): any;
    function lengthBytesUTF8(str: any): number;
    /**
     * Given a pointer 'ptr' to a null-terminated UTF8-encoded string in the
     * emscripten HEAP, returns a copy of that string as a Javascript String object.
     *
     * @param {number} ptr
     * @param {number=} maxBytesToRead - An optional length that specifies the
     *   maximum number of bytes to read. You can omit this parameter to scan the
     *   string until the first 0 byte. If maxBytesToRead is passed, and the string
     *   at [ptr, ptr+maxBytesToReadr[ contains a null byte in the middle, then the
     *   string will cut short at that byte index.
     * @param {boolean=} ignoreNul - If true, the function will not stop on a NUL character.
     * @return {string}
     */
    function UTF8ToString(ptr: number, maxBytesToRead?: number | undefined, ignoreNul?: boolean | undefined): string;
}
interface WasmModule {
  _main_getAppPointer(): number;
  _main_shutdown(): void;
  _main_start(_0: BigInt, _1: number, _2: number, _3: number, _4: number): void;
  _idContainer_iterator_dispose(_0: number): void;
  _idContainer_iterator_getCurrent_value(_0: number): number;
  _idContainer_iterator_getCurrent_key(_0: number): number;
  _idContainer_iterator_moveNext(_0: number): number;
  _idContainer_getIterator(_0: number): number;
  _idContainer_remove(_0: number, _1: number): number;
  _idContainer_insert(_0: number, _1: number): number;
  _idContainer_getById(_0: number, _1: number): number;
  _idContainer_getCount(_0: number): number;
  _interop_free(_0: number): void;
  _interop_alloc(_0: number): number;
  _interop_releasePointer(_0: number): void;
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
