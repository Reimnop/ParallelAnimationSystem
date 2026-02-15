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
}
interface WasmModule {
  _main_getAppPointer(): number;
  _main_shutdown(): void;
  _main_start(_0: BigInt, _1: number, _2: number, _3: number, _4: number): void;
  _interop_free(_0: number): void;
  _interop_alloc(_0: number): number;
  _interop_releasePointer(_0: number): void;
  _app_processFrame(_0: number, _1: number): void;
  _app_getBeatmapDataPointer(_0: number): number;
}

export type MainModule = WasmModule & typeof RuntimeExports;
export default function MainModuleFactory (options?: unknown): Promise<MainModule>;
