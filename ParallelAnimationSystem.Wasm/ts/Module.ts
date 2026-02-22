import type { MainModule } from "./mod/ParallelAnimationSystem.Wasm";
import { App } from "./App";
import type { BeatmapFormat } from "./data/BeatmapFormat";
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
    const ptr = this.wasm._main_getAppPointer();
    if (ptr === 0) {
      return null;
    }
    return new App(this, ptr);
  }

  start(seed: BigInt, enablePostProcessing: boolean, enableTextRendering: boolean, beatmapData: string, beatmapFormat: BeatmapFormat): void {
    // we allocate the beatmap data string on the heap
    // because beatmap data can be large, and we don't
    // want to risk overflowing the stack
    const beatmapDataPtr = this.interopHelper.stringToUTF8OnHeap(beatmapData);
    try {
      this.wasm._main_start(
          seed,
          enablePostProcessing ? 1 : 0,
          enableTextRendering ? 1 : 0,
          beatmapDataPtr,
          beatmapFormat);
    } finally {
      this.wasm._interop_free(beatmapDataPtr);
    }
  }

  shutdown(): void {
    this.wasm._main_shutdown();
  }
}