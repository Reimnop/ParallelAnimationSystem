import type { MainModule } from "./mod/ParallelAnimationSystem.Wasm";
import { App } from "./App";
import { BeatmapFormat } from "./BeatmapFormat";
import { MemoryManager } from "./MemoryManager";

export type WasmModule = MainModule & {
  canvas?: HTMLCanvasElement;
};

export class Module {
  wasm: WasmModule;
  memoryManager: MemoryManager;
  
  public constructor(wasm: WasmModule, canvas: HTMLCanvasElement) {
    this.wasm = wasm;
    this.memoryManager = new MemoryManager(this.wasm);
    
    this.wasm.canvas = canvas;
  }

  get app(): App | null {
    const ptr = this.wasm._main_getAppPointer();
    if (ptr === 0) {
      return null;
    }
    return new App(ptr, this);
  }

  start(seed: BigInt, enablePostProcessing: boolean, enableTextRendering: boolean, beatmapData: string, beatmapFormat: BeatmapFormat): void {
    const beatmapDataPtr = this.wasm.stringToNewUTF8(beatmapData) as number;
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