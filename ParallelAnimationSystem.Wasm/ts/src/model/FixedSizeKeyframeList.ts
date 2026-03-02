import { NativeObject } from "../NativeObject";
import type { Module } from "../Module";
import type { FixedSizeKeyframeCodec } from "./FixedSizeKeyframeCodec";
import type { KeyframeList } from "./KeyframeList";

export class FixedSizeKeyframeList<T> extends NativeObject implements KeyframeList<T> {
  
  private readonly adapter: FixedSizeKeyframeCodec<T>;
  
  public constructor(module: Module, ptr: number, adapter: FixedSizeKeyframeCodec<T>) {
    super(module, ptr);
    this.adapter = adapter;
  }
  
  get count(): number {
    return this.wasm._keyframeList_getCount(this.ptr);
  }
  
  fetchAt(index: number): T {
    const sp = this.wasm.stackSave();
    try {
      const bufferPtr = this.wasm.stackAlloc(this.adapter.size);
      this.wasm._keyframeList_fetchAt(this.ptr, bufferPtr, index);
      return this.adapter.read(bufferPtr);
    } finally {
      this.wasm.stackRestore(sp);
    }
  }
  
  fetchRange(start: number, count: number): T[] {
    const bufferSize = count * this.adapter.size;
    const bufferPtr = this.wasm._interop_alloc(bufferSize); // heap allocate to avoid stack overflow for large ranges
    try {
      this.wasm._keyframeList_fetchRange(this.ptr, bufferPtr, start, count);
      
      const keyframes: T[] = [];
      for (let i = 0; i < count; i++) {
        const value = this.adapter.read(bufferPtr + i * this.adapter.size);
        keyframes.push(value);
      }
      
      return keyframes;
    } finally {
      this.wasm._interop_free(bufferPtr);
    }
  }
  
  load(keyframes: T[]): void {
    const bufferSize = keyframes.length * this.adapter.size;
    const bufferPtr = this.wasm._interop_alloc(bufferSize); // heap allocate to avoid stack overflow for large arrays
    try {
      for (let i = 0; i < keyframes.length; i++) {
        this.adapter.write(keyframes[i], bufferPtr + i * this.adapter.size);
      }
      this.wasm._keyframeList_load(this.ptr, bufferPtr, keyframes.length);
    } finally {
      this.wasm._interop_free(bufferPtr);
    }
  }
  
  toArray(): T[] {
    return this.fetchRange(0, this.count);
  }
  
  *[Symbol.iterator](): Iterator<T> {
    const count = this.count;
    const sp = this.wasm.stackSave();
    try {
      const bufferPtr = this.wasm.stackAlloc(this.adapter.size);
      for (let i = 0; i < count; i++) {
        this.wasm._keyframeList_fetchAt(this.ptr, bufferPtr, i);
        yield this.adapter.read(bufferPtr);
      }
    } finally {
      this.wasm.stackRestore(sp);
    }
  }
}