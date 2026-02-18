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
    return this.module.wasm._keyframeList_getCount(this.ptr);
  }
  
  at(index: number): K {
    const keyframePtr = this.module.wasm._keyframeList_at(this.ptr, index);
    try {
      return this.deserialize(this.module, keyframePtr);
    } finally {
      this.module.wasm._interop_releasePointer(keyframePtr);
    }
  }
  
  add(keyframe: K): void {
    const keyframePtr = this.serialize(this.module, keyframe);
    try {
      this.module.wasm._keyframeList_add(this.ptr, keyframePtr);
    } finally {
      this.module.wasm._interop_releasePointer(keyframePtr);
    }
  }
  
  removeAt(index: number): void {
    this.module.wasm._keyframeList_removeAt(this.ptr, index);
  }
  
  replace(keyframes: K[]) {
    if (keyframes.length === 0) {
      this.module.wasm._keyframeList_replace(this.ptr, 0, 0);
      return;
    }
    
    const keyframePtrs = this.keyframeListToPtrs(keyframes);
    try {
      const ptrSize = 4; // assume 32-bit pointers
      const keyframePtrsPtr = this.module.wasm._interop_alloc(keyframePtrs.length * ptrSize);
      try {
        const baseU32Index = keyframePtrsPtr >> 2;
        for (let i = 0; i < keyframePtrs.length; i++) {
          this.module.wasm.HEAPU32[baseU32Index + i] = keyframePtrs[i];
        }
        this.module.wasm._keyframeList_replace(this.ptr, keyframePtrsPtr, keyframes.length);
      } finally {
        this.module.wasm._interop_free(keyframePtrsPtr);
      }
    } finally {
      for (const ptr of keyframePtrs) {
        this.module.wasm._interop_releasePointer(ptr);
      }
    }
  }
  
  *[Symbol.iterator](): Iterator<K> {
    const iteratorPtr = this.module.wasm._keyframeList_getIterator(this.ptr);
    try {
      while (this.module.wasm._keyframeList_iterator_moveNext(iteratorPtr) !== 0) {
        const keyframePtr = this.module.wasm._keyframeList_iterator_getCurrent(iteratorPtr);
        try {
          yield this.deserialize(this.module, keyframePtr);
        } finally {
          this.module.wasm._interop_releasePointer(keyframePtr);
        }
      }
    } finally {
      this.module.wasm._keyframeList_iterator_dispose(iteratorPtr);
      this.module.wasm._interop_releasePointer(iteratorPtr);
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
        this.module.wasm._interop_releasePointer(ptr);
      }
      throw e;
    }
    return result;
  }
}