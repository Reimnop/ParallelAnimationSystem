import { NativeObject } from "../NativeObject";
import { Module } from "../Module";
import type { BeatmapObjectType } from "../data/BeatmapObjectType";
import type { BeatmapObjectParentType } from "../data/BeatmapObjectParentType";
import type { BeatmapObjectParentOffset } from "../data/BeatmapObjectParentOffset";
import type { BeatmapObjectRenderType } from "../data/BeatmapObjectRenderType";
import type { Vector } from "../data/Vector";
import type { BeatmapObjectAutoKillType } from "../data/BeatmapObjectAutoKillType";
import type { BeatmapObjectShape } from "../data/BeatmapObjectShape";

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
  
  get parentId(): string | null {
    const parentIdPtr = this.module.wasm._beatmapObject_getParentId(this.ptr);
    if (parentIdPtr === 0) {
      return null;
    }
    try {
      return this.module.wasm.UTF8ToString(parentIdPtr);
    } finally {
      this.module.wasm._interop_free(parentIdPtr);
    }
  }
  
  set parentId(value: string | null) {
    const parentIdPtr = value !== null ? this.module.wasm.stringToNewUTF8(value) as number : 0;
    try {
      this.module.wasm._beatmapObject_setParentId(this.ptr, parentIdPtr);
    } finally {
      if (parentIdPtr !== 0) {
        this.module.wasm._interop_free(parentIdPtr);
      }
    }
  }
  
  get type(): BeatmapObjectType {
    return this.module.wasm._beatmapObject_getType(this.ptr);
  }
  
  set type(value: BeatmapObjectType) {
    this.module.wasm._beatmapObject_setType(this.ptr, value);
  }
  
  get parentType(): BeatmapObjectParentType {
    return this.module.wasm._beatmapObject_getParentType(this.ptr);
  }
  
  set parentType(value: BeatmapObjectParentType) {
    this.module.wasm._beatmapObject_setParentType(this.ptr, value);
  }
  
  get parentOffset(): BeatmapObjectParentOffset {
    const buf = this.module.wasm._interop_alloc(4 * 3); // allocate buffer for 3 f32 values
    try {
      this.module.wasm._beatmapObject_getParentOffset(this.ptr, buf);
      const position = this.module.wasm.HEAPF32[buf >> 2] as number;
      const scale = this.module.wasm.HEAPF32[(buf >> 2) + 1] as number;
      const rotation = this.module.wasm.HEAPF32[(buf >> 2) + 2] as number;
      return { position, scale, rotation };
    } finally {
      this.module.wasm._interop_free(buf);
    }
  }
  
  set parentOffset(value: BeatmapObjectParentOffset) {
    const buf = this.module.wasm._interop_alloc(4 * 3); // allocate buffer for 3 f32 values
    try {
      this.module.wasm.HEAPF32[buf >> 2] = value.position;
      this.module.wasm.HEAPF32[(buf >> 2) + 1] = value.scale;
      this.module.wasm.HEAPF32[(buf >> 2) + 2] = value.rotation;
      this.module.wasm._beatmapObject_setParentOffset(this.ptr, buf);
    } finally {
      this.module.wasm._interop_free(buf);
    }
  }
  
  get renderType(): BeatmapObjectRenderType {
    return this.module.wasm._beatmapObject_getRenderType(this.ptr);
  }
  
  set renderType(value: BeatmapObjectRenderType) {
    this.module.wasm._beatmapObject_setRenderType(this.ptr, value);
  }
  
  get origin(): Vector<2> {
    const buf = this.module.wasm._interop_alloc(4 * 2); // allocate buffer for 2 f32 values
    try {
      this.module.wasm._beatmapObject_getOrigin(this.ptr, buf);
      const x = this.module.wasm.HEAPF32[buf >> 2] as number;
      const y = this.module.wasm.HEAPF32[(buf >> 2) + 1] as number;
      return [x, y];
    } finally {
      this.module.wasm._interop_free(buf);
    }
  }
  
  set origin(value: Vector<2>) {
    const buf = this.module.wasm._interop_alloc(4 * 2); // allocate buffer for 2 f32 values
    try {
      this.module.wasm.HEAPF32[buf >> 2] = value[0];
      this.module.wasm.HEAPF32[(buf >> 2) + 1] = value[1];
      this.module.wasm._beatmapObject_setOrigin(this.ptr, buf);
    } finally {
      this.module.wasm._interop_free(buf);
    }
  }
  
  get renderDepth(): number {
    return this.module.wasm._beatmapObject_getRenderDepth(this.ptr);
  }
  
  set renderDepth(value: number) {
    this.module.wasm._beatmapObject_setRenderDepth(this.ptr, value);
  }
  
  get startTime(): number {
    return this.module.wasm._beatmapObject_getStartTime(this.ptr);
  }
  
  set startTime(value: number) {
    this.module.wasm._beatmapObject_setStartTime(this.ptr, value);
  }
  
  get autoKillType(): BeatmapObjectAutoKillType {
    return this.module.wasm._beatmapObject_getAutoKillType(this.ptr);
  }
  
  set autoKillType(value: BeatmapObjectAutoKillType) {
    this.module.wasm._beatmapObject_setAutoKillType(this.ptr, value);
  }
  
  get autoKillOffset(): number {
    return this.module.wasm._beatmapObject_getAutoKillOffset(this.ptr);
  }
  
  set autoKillOffset(value: number) {
    this.module.wasm._beatmapObject_setAutoKillOffset(this.ptr, value);
  }
  
  get shape(): BeatmapObjectShape {
    return this.module.wasm._beatmapObject_getShape(this.ptr);
  }
  
  set shape(value: BeatmapObjectShape) {
    this.module.wasm._beatmapObject_setShape(this.ptr, value);
  }
  
  get text(): string | null {
    const textPtr = this.module.wasm._beatmapObject_getText(this.ptr);
    if (textPtr === 0) {
      return null;
    }
    try {
      return this.module.wasm.UTF8ToString(textPtr);
    } finally {
      this.module.wasm._interop_free(textPtr);
    }
  }
  
  set text(value: string | null) {
    const textPtr = value !== null ? this.module.wasm.stringToNewUTF8(value) as number : 0;
    try {
      this.module.wasm._beatmapObject_setText(this.ptr, textPtr);
    } finally {
      if (textPtr !== 0) {
        this.module.wasm._interop_free(textPtr);
      }
    }
  }
}