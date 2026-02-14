import type { MainModule } from "./ParallelAnimationSystem.Wasm";
import { PASApp } from "./PASApp";
import { PASBeatmapFormat } from "./PASBeatmapFormat";
import { PASMemoryManager } from "./PASMemoryManager";

export type PASWasmModule = MainModule & {
  canvas?: HTMLCanvasElement;
};

export class PASModule {
  wasm: PASWasmModule;
  memoryManager: PASMemoryManager;
  
  public constructor(wasm: PASWasmModule, canvas: HTMLCanvasElement) {
    this.wasm = wasm;
    this.memoryManager = new PASMemoryManager(this.wasm);
    
    this.wasm.canvas = canvas;
  }

  start(seed: BigInt, enablePostProcessing: boolean, enableTextRendering: boolean, beatmapData: string, beatmapFormat: PASBeatmapFormat): void {
    const beatmapDataPtr = this.wasm.stringToNewUTF8(beatmapData) as number;
    try {
      this.wasm._main_start(
          seed,
          enablePostProcessing ? 1 : 0,
          enableTextRendering ? 1 : 0,
          beatmapDataPtr,
          beatmapFormat);
    } finally {
      this.wasm._free(beatmapDataPtr);
    }
  }

  shutdown(): void {
    this.wasm._main_shutdown();
  }

  getApp(): PASApp | null {
    const ptr = this.wasm._main_getAppPointer();
    if (ptr === 0) {
      return null;
    }
    return new PASApp(ptr, this);
  }
}