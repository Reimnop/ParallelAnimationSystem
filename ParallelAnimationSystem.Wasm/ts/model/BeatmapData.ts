import { NativeObject } from "../NativeObject";
import { IdContainer } from "./IdContainer";
import { BeatmapObject } from "./BeatmapObject";
import { BeatmapPrefabInstance } from "./BeatmapPrefabInstance";
import { BeatmapPrefab } from "./BeatmapPrefab";

export class BeatmapData extends NativeObject {
  get objects(): IdContainer<BeatmapObject> {
    const ptr = this.wasm._beatmapData_getObjects(this.ptr);
    return new IdContainer(this.module, ptr, BeatmapObject);
  }
  
  get prefabInstances(): IdContainer<BeatmapPrefabInstance> {
    const ptr = this.wasm._beatmapData_getPrefabInstances(this.ptr);
    return new IdContainer(this.module, ptr, BeatmapPrefabInstance);
  }
  
  get prefabs(): IdContainer<BeatmapPrefab> {
    const ptr = this.wasm._beatmapData_getPrefabs(this.ptr);
    return new IdContainer(this.module, ptr, BeatmapPrefab);
  }
}