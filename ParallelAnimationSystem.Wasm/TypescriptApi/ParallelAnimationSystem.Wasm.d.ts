// TypeScript bindings for emscripten-generated code.  Automatically generated at compile time.
declare namespace RuntimeExports {
    function stringToNewUTF8(str: any): any;
}
interface WasmModule {
    _free(_0: number): void;
    _malloc(_0: number): number;
    _main_releaseAppPointer(_0: number): void;
    _main_getAppPointer(): number;
    _main_shutdown(): void;
    _main_start(_0: BigInt, _1: number, _2: number, _3: number, _4: number): void;
    _app_processFrame(_0: number, _1: number): void;
}

export type MainModule = WasmModule & typeof RuntimeExports;
export default function MainModuleFactory (options?: unknown): Promise<MainModule>;
