import type { MainModule } from "./wasm/ParallelAnimationSystem.Wasm";
import { App } from "./App";
import { MemoryManager } from "./MemoryManager";
import { InteropHelper } from "./InteropHelper";

type Override<T, R> = Omit<T, keyof R> & R;

interface ModuleExports {
  HEAP_DATA_VIEW: DataView;
  HEAP8: Int8Array;
  HEAPU8: Uint8Array;
  HEAP16: Int16Array;
  HEAPU16: Uint16Array;
  HEAP32: Int32Array;
  HEAPU32: Uint32Array;
  HEAPF32: Float32Array;
  HEAP64: BigInt64Array;
  HEAPU64: BigUint64Array;
  HEAPF64: Float64Array;
  stackSave(): number;
  stackRestore(ptr: number): void;
  stackAlloc(sz: number): number;
  stringToUTF8(str: string, outPtr: number, maxBytesToWrite: number): number;
  lengthBytesUTF8(s: string): number;
}

interface ModuleBrowserExports {
  canvas?: HTMLCanvasElement;
}

export type WasmModule = Override<MainModule, ModuleExports> & ModuleBrowserExports;

export class Module {
  wasm: WasmModule;
  memoryManager: MemoryManager;
  interopHelper: InteropHelper;
  
  public constructor(wasm: WasmModule, canvas: HTMLCanvasElement) {
    this.wasm = wasm;
    this.memoryManager = new MemoryManager(this.wasm);
    this.interopHelper = new InteropHelper(this.wasm);
    
    this.wasm.canvas = canvas;
  }

  get app(): App | null {
    const ptr = this.wasm._main_getApp();
    if (ptr === 0) {
      return null;
    }
    return new App(this, ptr);
  }

  start(enablePostProcessing: boolean, enableTextRendering: boolean): void {
    this.wasm._main_start(
      enablePostProcessing ? 1 : 0,
      enableTextRendering ? 1 : 0);
  }

  shutdown(): void {
    this.wasm._main_shutdown();
  }
}