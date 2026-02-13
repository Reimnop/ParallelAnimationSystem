import { MainModule } from "./ParallelAnimationSystem.Wasm";
import { PASApp } from "./PASApp";
import { PASBeatmapFormat } from "./PASBeatmapFormat";

export type PASWasmModule = MainModule & {
  canvas?: HTMLCanvasElement;
};

export class PASModule {
  private readonly instance: PASWasmModule;
  
  public constructor(instance: PASWasmModule, canvas: HTMLCanvasElement) {
    this.instance = instance;
    this.instance.canvas = canvas;
  }

  start(seed: BigInt, enablePostProcessing: boolean, enableTextRendering: boolean, beatmapData: string, beatmapFormat: PASBeatmapFormat): void {
    const beatmapDataPtr = this.instance.stringToNewUTF8(beatmapData) as number;
    this.instance._main_start(
      seed,
      enablePostProcessing ? 1 : 0,
      enableTextRendering ? 1 : 0,
      beatmapDataPtr,
      beatmapFormat);
    this.instance._free(beatmapDataPtr);
  }

  shutdown(): void {
    this.instance._main_shutdown();
  }

  getApp(): PASApp | null {
    const ptr = this.instance._main_getAppPointer();
    if (ptr === 0)
      return null;
    return new PASApp(this.instance, ptr);
  }
}