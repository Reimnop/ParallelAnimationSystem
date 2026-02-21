import type { Keyframe, RandomizableKeyframe } from "../data/Keyframe";
import type { Module } from "../Module";
import type { StructCodec } from "./StructCodec";

// codec for fixed-size keyframe value types
export interface FixedSizeKeyframeCodec<T> {
  size: number;
  write(value: T, ptr: number): void;
  read(ptr: number): T;
}

export class RandomizableKeyframeCodec<T>
  implements FixedSizeKeyframeCodec<RandomizableKeyframe<T>>
{
  size: number;

  private readonly module: Module;
  private readonly structCodec: StructCodec<T>;

  public constructor(module: Module, structCodec: StructCodec<T>) {
    this.module = module;
    this.structCodec = structCodec;

    // time (4) + ease (4) + value + randomMode (4) + randomValue + randomInterval (4) + isRelative (1)
    this.size =
      4 + 4 +
      structCodec.size +
      4 +
      structCodec.size +
      4 +
      1;
  }

  write(keyframe: RandomizableKeyframe<T>, ptr: number): void {
    const view = this.module.wasm.HEAP_DATA_VIEW;
    let p = ptr;

    view.setFloat32(p, keyframe.time, true);
    p += 4;

    view.setInt32(p, keyframe.ease, true);
    p += 4;

    this.structCodec.write(view, keyframe.value, p);
    p += this.structCodec.size;

    view.setInt32(p, keyframe.randomMode, true);
    p += 4;

    this.structCodec.write(view, keyframe.randomValue, p);
    p += this.structCodec.size;

    view.setInt32(p, keyframe.randomInterval, true);
    p += 4;

    view.setUint8(p, keyframe.isRelative ? 1 : 0);
  }

  read(ptr: number): RandomizableKeyframe<T> {
    const view = this.module.wasm.HEAP_DATA_VIEW;
    let p = ptr;

    const time = view.getFloat32(p, true);
    p += 4;

    const ease = view.getInt32(p, true);
    p += 4;

    const value = this.structCodec.read(view, p);
    p += this.structCodec.size;

    const randomMode = view.getInt32(p, true);
    p += 4;

    const randomValue = this.structCodec.read(view, p);
    p += this.structCodec.size;

    const randomInterval = view.getInt32(p, true);
    p += 4;

    const isRelative = view.getUint8(p) !== 0;

    return {
      time,
      ease,
      value,
      randomMode,
      randomValue,
      randomInterval,
      isRelative,
    };
  }
}

export class KeyframeCodec<T>
  implements FixedSizeKeyframeCodec<Keyframe<T>>
{
  size: number;

  private readonly module: Module;
  private readonly structCodec: StructCodec<T>;

  public constructor(module: Module, structCodec: StructCodec<T>) {
    this.module = module;
    this.structCodec = structCodec;

    // time (4) + ease (4) + value
    this.size = 4 + 4 + structCodec.size;
  }

  write(keyframe: Keyframe<T>, ptr: number): void {
    const view = this.module.wasm.HEAP_DATA_VIEW;
    let p = ptr;

    view.setFloat32(p, keyframe.time, true);
    p += 4;

    view.setInt32(p, keyframe.ease, true);
    p += 4;

    this.structCodec.write(view, keyframe.value, p);
  }

  read(ptr: number): Keyframe<T> {
    const view = this.module.wasm.HEAP_DATA_VIEW;
    let p = ptr;

    const time = view.getFloat32(p, true);
    p += 4;

    const ease = view.getInt32(p, true);
    p += 4;

    const value = this.structCodec.read(view, p);

    return { time, ease, value };
  }
}