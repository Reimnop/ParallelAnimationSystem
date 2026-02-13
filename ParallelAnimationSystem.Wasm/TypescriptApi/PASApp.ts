import { PASWasmModule } from "./PASModule";
import { NativeObject } from "./NativeObject";
import { PASBeatmapData } from "./PASBeatmapData";

export class PASApp implements NativeObject {
  ptr: number;
  
  private readonly module: PASWasmModule;

  public constructor(ptr: number, module: PASWasmModule) {
    this.ptr = ptr;
    this.module = module;
  }

  getBeatmapData(): PASBeatmapData {
    const ptr = this.module._app_getBeatmapDataPointer(this.ptr);
    return new PASBeatmapData(ptr, this.module);
  }

  processFrame(time: number): void {
    this.module._app_processFrame(this.ptr, time);
  }
}