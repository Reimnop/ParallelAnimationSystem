import { NativeObject } from "../NativeObject";
import type { Module } from "../Module";
import type { Vector } from "../data/Vector";

export class BeatmapPrefabInstance extends NativeObject {
  static create(module: Module, id: string): BeatmapPrefabInstance {
    const sp = module.wasm.stackSave();
    try {
      const idPtr = module.interopHelper.stringToUTF8OnStack(id);
      const ptr = module.wasm._beatmapPrefabInstance_new(idPtr);
      return new BeatmapPrefabInstance(module, ptr);
    } finally {
      module.wasm.stackRestore(sp);
    }
  }
  
  get id(): string {
    return this.interopHelper.getStringFromObjectNotNull(this.ptr, this.wasm._beatmapPrefabInstance_getId);
  }
  
  get startTime(): number {
    return this.wasm._beatmapPrefabInstance_getStartTime(this.ptr);
  }
  
  set startTime(value: number) {
    this.wasm._beatmapPrefabInstance_setStartTime(this.ptr, value);
  }
  
  get position(): Vector<2> {
    const sp = this.wasm.stackSave();
    try {
      const buf = this.wasm.stackAlloc(8);
      this.wasm._beatmapPrefabInstance_getPosition(this.ptr, buf);
      const x = this.wasm.HEAP_DATA_VIEW.getFloat32(buf, true);
      const y = this.wasm.HEAP_DATA_VIEW.getFloat32(buf + 4, true);
      return [x, y];
    } finally {
      this.wasm.stackRestore(sp);
    }
  }
  
  set position(value: Vector<2>) {
    const sp = this.wasm.stackSave();
    try {
      const buf = this.wasm.stackAlloc(8);
      this.wasm.HEAP_DATA_VIEW.setFloat32(buf, value[0], true);
      this.wasm.HEAP_DATA_VIEW.setFloat32(buf + 4, value[1], true);
      this.wasm._beatmapPrefabInstance_setPosition(this.ptr, buf);
    } finally {
      this.wasm.stackRestore(sp);
    }
  }
  
  get scale(): Vector<2> {
    const sp = this.wasm.stackSave();
    try {
      const buf = this.wasm.stackAlloc(8);
      this.wasm._beatmapPrefabInstance_getScale(this.ptr, buf);
      const x = this.wasm.HEAP_DATA_VIEW.getFloat32(buf, true);
      const y = this.wasm.HEAP_DATA_VIEW.getFloat32(buf + 4, true);
      return [x, y];
    } finally {
      this.wasm.stackRestore(sp);
    }
  }
  
  set scale(value: Vector<2>) {
    const sp = this.wasm.stackSave();
    try {
      const buf = this.wasm.stackAlloc(8);
      this.wasm.HEAP_DATA_VIEW.setFloat32(buf, value[0], true);
      this.wasm.HEAP_DATA_VIEW.setFloat32(buf + 4, value[1], true);
      this.wasm._beatmapPrefabInstance_setScale(this.ptr, buf);
    } finally {
      this.wasm.stackRestore(sp);
    }
  }

  get rotation(): number {
    return this.wasm._beatmapPrefabInstance_getRotation(this.ptr);
  }

  set rotation(value: number) {
    this.wasm._beatmapPrefabInstance_setRotation(this.ptr, value);
  }

  get prefabId(): string | null {
    return this.interopHelper.getStringFromObject(this.ptr, this.wasm._beatmapPrefabInstance_getPrefabId);
  }

  set prefabId(value: string | null) {
    this.interopHelper.setStringToObject(this.ptr, value, this.wasm._beatmapPrefabInstance_setPrefabId);
  }
}