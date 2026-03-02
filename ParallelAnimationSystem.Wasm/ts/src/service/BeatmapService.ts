import { NativeObject } from "../NativeObject";
import { BeatmapData } from "../model/BeatmapData";
import type { BeatmapFormat } from "../data/BeatmapFormat";

export class BeatmapService extends NativeObject {
  get beatmapData(): BeatmapData {
    const ptr = this.wasm._beatmapService_getBeatmapData(this.ptr);
    return new BeatmapData(this.module, ptr);
  }
  
  get beatmapFormat(): BeatmapFormat {
    const formatInt = this.wasm._beatmapService_getBeatmapFormat(this.ptr);
    return formatInt as BeatmapFormat;
  }
  
  loadBeatmap(beatmapData: string, beatmapFormat: BeatmapFormat): void {
    // we allocate the beatmap data string on the heap
    // because beatmap data can be large, and we don't
    // want to risk overflowing the stack
    const beatmapDataPtr = this.interopHelper.stringToUTF8OnHeap(beatmapData);
    try {
      this.wasm._beatmapService_loadBeatmap(this.ptr, beatmapDataPtr, beatmapFormat);
    } finally {
      this.wasm._interop_free(beatmapDataPtr);
    }
  }
}