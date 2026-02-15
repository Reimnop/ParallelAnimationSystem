import { PASModule } from "./PASModule";
import type { PASNativeObject } from "./PASNativeObject";
import { PASBeatmapData } from "./PASBeatmapData";

export class PASApp implements PASNativeObject {
  ptr: number;
  
  private readonly module: PASModule;

  public constructor(ptr: number, module: PASModule) {
    this.ptr = ptr;
    this.module = module;
    
    this.module.memoryManager.register(this);
  }

  release(): void {
    this.module.memoryManager.release(this);
  }

  getBeatmapData(): PASBeatmapData {
    const ptr = this.module.wasm._app_getBeatmapDataPointer(this.ptr);
    return new PASBeatmapData(ptr, this.module);
  }

  processFrame(time: number): void {
    this.module.wasm._app_processFrame(this.ptr, time);
  }
}