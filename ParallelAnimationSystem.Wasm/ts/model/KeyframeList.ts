import type { Keyframe } from "../data/Keyframe";
import { NativeObject } from "../NativeObject";
import type { Module } from "../Module";

export type KeyframeSerializer<T, K extends Keyframe<T>> = (module: Module, keyframe: K) => number;
export type KeyframeDeserializer<T, K extends Keyframe<T>> = (module: Module, ptr: number) => K;

export class KeyframeList<T, K extends Keyframe<T>> extends NativeObject {
  
  private readonly serialize: KeyframeSerializer<T, K>;
  private readonly deserialize: KeyframeDeserializer<T, K>;
  
  public constructor(
    serialize: KeyframeSerializer<T, K>,
    deserialize: KeyframeDeserializer<T, K>,
    module: Module, ptr: number) {
    super(module, ptr);
    this.serialize = serialize;
    this.deserialize = deserialize;
  }
  
  get count(): number {
    return this.wasm._keyframeList_getCount(this.ptr);
  }
  
  at(index: number): K {
    const keyframePtr = this.wasm._keyframeList_at(this.ptr, index);
    try {
      return this.deserialize(this.module, keyframePtr);
    } finally {
      this.wasm._interop_releasePointer(keyframePtr);
    }
  }
  
  add(keyframe: K): void {
    const keyframePtr = this.serialize(this.module, keyframe);
    try {
      this.wasm._keyframeList_add(this.ptr, keyframePtr);
    } finally {
      this.wasm._interop_releasePointer(keyframePtr);
    }
  }
  
  removeAt(index: number): void {
    this.wasm._keyframeList_removeAt(this.ptr, index);
  }
  
  replace(keyframes: K[]) {
    if (keyframes.length === 0) {
      this.wasm._keyframeList_replace(this.ptr, 0, 0);
      return;
    }
    
    const keyframePtrs = this.keyframeListToPtrs(keyframes);
    try {
      const ptrSize = 4; // assume 32-bit pointers
      const sp = this.wasm.stackSave();
      try {
        const keyframePtrsPtr = this.wasm.stackAlloc(keyframePtrs.length * ptrSize);
        for (let i = 0; i < keyframePtrs.length; i++) {
          this.wasm.HEAP_DATA_VIEW.setInt32(keyframePtrsPtr + i * ptrSize, keyframePtrs[i], true);
        }
        this.wasm._keyframeList_replace(this.ptr, keyframePtrsPtr, keyframes.length);
      } finally {
        this.wasm.stackRestore(sp);
      }
    } finally {
      for (const ptr of keyframePtrs) {
        this.wasm._interop_releasePointer(ptr);
      }
    }
  }
  
  *[Symbol.iterator](): Iterator<K> {
    const iteratorPtr = this.wasm._keyframeList_getIterator(this.ptr);
    try {
      while (this.wasm._keyframeList_iterator_moveNext(iteratorPtr) !== 0) {
        const keyframePtr = this.wasm._keyframeList_iterator_getCurrent(iteratorPtr);
        try {
          yield this.deserialize(this.module, keyframePtr);
        } finally {
          this.wasm._interop_releasePointer(keyframePtr);
        }
      }
    } finally {
      this.wasm._keyframeList_iterator_dispose(iteratorPtr);
      this.wasm._interop_releasePointer(iteratorPtr);
    }
  }
  
  private keyframeListToPtrs(keyframes: K[]): number[] {
    const result: number[] = [];
    try {
      for (const keyframe of keyframes) {
        const keyframePtr = this.serialize(this.module, keyframe);
        result.push(keyframePtr);
      }
    } catch(e) {
      for (const ptr of result) {
        this.wasm._interop_releasePointer(ptr);
      }
      throw e;
    }
    return result;
  }
}