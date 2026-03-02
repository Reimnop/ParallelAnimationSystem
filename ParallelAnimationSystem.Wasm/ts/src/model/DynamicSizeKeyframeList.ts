import { NativeObject } from "../NativeObject";
import type { Module } from "../Module";
import type { KeyframeList } from "./KeyframeList";
import type { DynamicSizeKeyframeCodec } from "./DynamicSizeKeyframeCodec";

export class DynamicSizeKeyframeList<T> extends NativeObject implements KeyframeList<T> {
  
  private readonly adapter: DynamicSizeKeyframeCodec<T>;
  
  public constructor(module: Module, ptr: number, adapter: DynamicSizeKeyframeCodec<T>) {
    super(module, ptr);
    this.adapter = adapter;
  }
  
  get count(): number {
    return this.wasm._keyframeList_getCount(this.ptr);
  }
  
  fetchAt(index: number): T {
    const sp = this.wasm.stackSave();
    try {
      const size = this.wasm._keyframeList_getKeyframeSize(this.ptr, index);
      const bufferPtr = this.wasm.stackAlloc(size);
      this.wasm._keyframeList_fetchAt(this.ptr, bufferPtr, index);
      return this.adapter.read(bufferPtr).value;
    } finally {
      this.wasm.stackRestore(sp);
    }
  }
  
  fetchRange(start: number, count: number): T[] {
    const bufferSize = this.wasm._keyframeList_getBufferSize(this.ptr, start, count);
    const bufferPtr = this.wasm._interop_alloc(bufferSize); // heap allocate to avoid stack overflow for large ranges
    try {
      this.wasm._keyframeList_fetchRange(this.ptr, bufferPtr, start, count);
      
      let currentBufferPtr = bufferPtr;
      const keyframes: T[] = [];
      for (let i = 0; i < count; i++) {
        const { value, bytesRead } = this.adapter.read(currentBufferPtr);
        keyframes.push(value);
        currentBufferPtr += bytesRead;
      }
      
      return keyframes;
    } finally {
      this.wasm._interop_free(bufferPtr);
    }
  }
  
  load(keyframes: T[]): void {
    const bufferSize = this.calculateBufferSize(keyframes);
    const bufferPtr = this.wasm._interop_alloc(bufferSize); // heap allocate to avoid stack overflow for large arrays
    try {
      let currentBufferPtr = bufferPtr;
      for (let i = 0; i < keyframes.length; i++) {
        const bytesWritten = this.adapter.write(keyframes[i], currentBufferPtr);
        currentBufferPtr += bytesWritten;
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
    for (let i = 0; i < count; i++) {
      const sp = this.wasm.stackSave();
      try {
        const size = this.wasm._keyframeList_getKeyframeSize(this.ptr, i);
        const bufferPtr = this.wasm.stackAlloc(size);
        this.wasm._keyframeList_fetchAt(this.ptr, bufferPtr, i);
        yield this.adapter.read(bufferPtr).value;
      } finally {
        this.wasm.stackRestore(sp);
      }
    }
  }
  
  private calculateBufferSize(keyframes: T[]): number {
    let totalSize = 0;
    for (const keyframe of keyframes) {
      totalSize += this.adapter.getSize(keyframe);
    }
    return totalSize;
  }
}