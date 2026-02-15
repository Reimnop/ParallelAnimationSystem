import { NativeObject } from "../NativeObject";
import { IdContainer } from "../data/IdContainer";
import { BeatmapObject } from "./BeatmapObject";

export class BeatmapData extends NativeObject {
  get objects(): IdContainer<BeatmapObject> {
    const ptr = this.module.wasm._beatmapData_getObjects(this.ptr);
    return new IdContainer(BeatmapObject, ptr, this.module);
  }
}