import { NativeObject } from "./NativeObject";
import { BeatmapData } from "./model/BeatmapData";

export class App extends NativeObject {
  get beatmapData(): BeatmapData {
    const ptr = this.module.wasm._app_getBeatmapDataPointer(this.ptr);
    return new BeatmapData(ptr, this.module);
  }

  processFrame(time: number): void {
    this.module.wasm._app_processFrame(this.ptr, time);
  }
}