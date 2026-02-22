import { NativeObject } from "./NativeObject";
import { BeatmapData } from "./model/BeatmapData";

export class App extends NativeObject {
  get beatmapData(): BeatmapData {
    const ptr = this.wasm._app_getBeatmapDataPointer(this.ptr);
    return new BeatmapData(this.module, ptr);
  }

  processFrame(time: number): void {
    this.wasm._app_processFrame(this.ptr, time);
  }
}