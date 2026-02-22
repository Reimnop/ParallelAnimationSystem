import { NativeObject } from "../NativeObject";
import type { Module } from "../Module";
import { IdContainer } from "./IdContainer";
import { BeatmapObject } from "./BeatmapObject";

export class BeatmapPrefab extends NativeObject {
  static create(module: Module, id: string): BeatmapPrefab {
    const sp = module.wasm.stackSave();
    try {
      const idPtr = module.interopHelper.stringToUTF8OnStack(id);
      const ptr = module.wasm._beatmapPrefab_new(idPtr);
      return new BeatmapPrefab(module, ptr);
    } finally {
      module.wasm.stackRestore(sp);
    }
  }
  
  get id(): string {
    return this.interopHelper.getStringFromObjectNotNull(this.ptr, this.wasm._beatmapPrefab_getId);
  }
  
  get name(): string {
    return this.interopHelper.getStringFromObjectNotNull(this.ptr, this.wasm._beatmapPrefab_getName);
  }
  
  set name(value: string) {
    this.interopHelper.setStringToObject(this.ptr, value, this.wasm._beatmapPrefab_setName);
  }
  
  get offset(): number {
    return this.wasm._beatmapPrefab_getOffset(this.ptr);
  }
  
  set offset(value: number) {
    this.wasm._beatmapPrefab_setOffset(this.ptr, value);
  }
  
  get objects(): IdContainer<BeatmapObject> {
    const ptr = this.wasm._beatmapPrefab_getObjects(this.ptr);
    return new IdContainer(this.module, ptr, BeatmapObject);
  }
}