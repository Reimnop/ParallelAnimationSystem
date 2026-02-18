import { NativeObject } from "../NativeObject";
import { IdContainer } from "./IdContainer";
import { BeatmapObject } from "./BeatmapObject";

export class BeatmapData extends NativeObject {
  get objects(): IdContainer<BeatmapObject> {
    const ptr = this.module.wasm._beatmapData_getObjects(this.ptr);
    return new IdContainer(BeatmapObject, this.module, ptr);
  }
}