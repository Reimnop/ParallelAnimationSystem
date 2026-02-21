import type { Keyframe, RandomizableKeyframe } from "../data/Keyframe";
import type { Module } from "../Module";
import type { StructCodec } from "./StructCodec";

// codec for fixed-size keyframe value types
export interface FixedSizeKeyframeCodec<T> {
  size: number;
  write(keyframe: T, ptr: number): void;
  read(ptr: number): T;
}

export class RandomizableKeyframeCodec<T> implements FixedSizeKeyframeCodec<RandomizableKeyframe<T>> {
  size: number;
  
  private readonly module: Module;
  private readonly structCodec: StructCodec<T>;
  
  public constructor(module: Module, structCodec: StructCodec<T>) {
    this.module = module;
    this.structCodec = structCodec;
    
    // time (4) + ease (4) + value (structCodec.size) + randomMode (4) + randomValue (structCodec.size) + randomInterval (4) + isRelative (1)
    this.size = 4 + 4 + structCodec.size + 4 + structCodec.size + 4 + 1;
  }
  
  write(keyframe: RandomizableKeyframe<T>, ptr: number): void {
    const dataView = this.module.wasm.HEAP_DATA_VIEW;
    dataView.setFloat32(ptr += 4, keyframe.time, true);
    dataView.setInt32(ptr += 4, keyframe.ease, true);
    this.structCodec.write(dataView, keyframe.value, ptr += this.structCodec.size);
    dataView.setInt32(ptr += 4, keyframe.randomMode, true);
    this.structCodec.write(dataView, keyframe.randomValue, ptr += this.structCodec.size);
    dataView.setInt32(ptr += 4, keyframe.randomInterval, true);
    dataView.setUint8(ptr, keyframe.isRelative ? 1 : 0);
  }
  
  read(ptr: number): RandomizableKeyframe<T> {
    const dataView = this.module.wasm.HEAP_DATA_VIEW;
    const time = dataView.getFloat32(ptr += 4, true);
    const ease = dataView.getInt32(ptr += 4, true);
    const value = this.structCodec.read(dataView, ptr += this.structCodec.size);
    const randomMode = dataView.getInt32(ptr += 4, true);
    const randomValue = this.structCodec.read(dataView, ptr += this.structCodec.size);
    const randomInterval = dataView.getInt32(ptr += 4, true);
    const isRelative = dataView.getUint8(ptr) !== 0;

    return {
      time,
      ease,
      value,
      randomMode,
      randomValue,
      randomInterval,
      isRelative
    };
  }
}

export class KeyframeCodec<T> implements FixedSizeKeyframeCodec<Keyframe<T>> {
  size: number;

  private readonly module: Module;
  private readonly structCodec: StructCodec<T>;

  public constructor(module: Module, structCodec: StructCodec<T>) {
    this.module = module;
    this.structCodec = structCodec;
    
    // time (4) + ease (4) + value (structCodec.size)
    this.size = 4 + 4 + structCodec.size;
  }

  write(keyframe: Keyframe<T>, ptr: number): void {
    const dataView = this.module.wasm.HEAP_DATA_VIEW;
    dataView.setFloat32(ptr += 4, keyframe.time, true);
    dataView.setInt32(ptr += 4, keyframe.ease, true);
    this.structCodec.write(dataView, keyframe.value, ptr);
  }

  read(ptr: number): Keyframe<T> {
    const dataView = this.module.wasm.HEAP_DATA_VIEW;
    const time = dataView.getFloat32(ptr += 4, true);
    const ease = dataView.getInt32(ptr += 4, true);
    const value = this.structCodec.read(dataView, ptr);
    return { time, ease, value };
  }
}