import { NativeObject } from "../NativeObject";
import { Module } from "../Module";

export class BeatmapObject extends NativeObject {
  static create(id: string, module: Module): BeatmapObject {
    const idPtr = module.wasm.stringToNewUTF8(id) as number;
    try {
      const ptr = module.wasm._beatmapObject_new(idPtr);
      return new BeatmapObject(ptr, module);
    } finally {
      module.wasm._interop_free(idPtr);
    }
  }
  
  get id(): string {
    const idPtr = this.module.wasm._beatmapObject_getId(this.ptr);
    try {
      return this.module.wasm.UTF8ToString(idPtr);
    } finally {
      this.module.wasm._interop_free(idPtr);
    }
  }
  
  get name(): string {
    const namePtr = this.module.wasm._beatmapObject_getName(this.ptr);
    try {
      return this.module.wasm.UTF8ToString(namePtr);
    } finally {
      this.module.wasm._interop_free(namePtr);
    }
  }
  
  set name(value: string) {
    const namePtr = this.module.wasm.stringToNewUTF8(value) as number;
    try {
      this.module.wasm._beatmapObject_setName(this.ptr, namePtr);
    } finally {
      this.module.wasm._interop_free(namePtr);
    }
  }
}