import type { Keyframe, RandomizableKeyframe } from "../data/Keyframe";
import type { Vector } from "../data/Vector";
import type { Module } from "../Module";
import type { BeatmapObjectIndexedColor } from "../data/BeatmapObjectIndexedColor";

// adapter for fixed-size keyframe value types
export interface KeyframeAdapter<T> {
  size: number;
  write(keyframe: T, ptr: number): void;
  read(ptr: number): T;
}

export class RandomizableKeyframeVector2Adapter implements KeyframeAdapter<RandomizableKeyframe<Vector<2>>> {
  size: number = 36;
  
  private readonly module: Module;
  
  public constructor(module: Module) {
    this.module = module;
  }
  
  write(keyframe: RandomizableKeyframe<Vector<2>>, ptr: number): void {
    const dataView = this.module.wasm.HEAP_DATA_VIEW;
    dataView.setFloat32(ptr, keyframe.time, true);
    dataView.setInt32(ptr + 4, keyframe.ease, true);
    dataView.setFloat32(ptr + 8, keyframe.value[0], true);
    dataView.setFloat32(ptr + 12, keyframe.value[1], true);
    dataView.setInt32(ptr + 16, keyframe.randomMode, true);
    dataView.setFloat32(ptr + 20, keyframe.randomValue[0], true);
    dataView.setFloat32(ptr + 24, keyframe.randomValue[1], true);
    dataView.setInt32(ptr + 28, keyframe.randomInterval, true);
    dataView.setInt32(ptr + 32, keyframe.isRelative ? 1 : 0, true);
  }
  
  read(ptr: number): RandomizableKeyframe<Vector<2>> {
    const dataView = this.module.wasm.HEAP_DATA_VIEW;
    const time = dataView.getFloat32(ptr, true);
    const ease = dataView.getInt32(ptr + 4, true);
    const valueX = dataView.getFloat32(ptr + 8, true);
    const valueY = dataView.getFloat32(ptr + 12, true);
    const randomMode = dataView.getInt32(ptr + 16, true);
    const randomValueX = dataView.getFloat32(ptr + 20, true);
    const randomValueY = dataView.getFloat32(ptr + 24, true);
    const randomInterval = dataView.getInt32(ptr + 28, true);
    const isRelative = dataView.getInt32(ptr + 32, true) !== 0;
    
    return {
      time,
      ease,
      value: [valueX, valueY],
      randomMode,
      randomValue: [randomValueX, randomValueY],
      randomInterval,
      isRelative
    };
  }
}

export class RandomizableKeyframeFloatAdapter implements KeyframeAdapter<RandomizableKeyframe<number>> {
  size: number = 28;

  private readonly module: Module;

  public constructor(module: Module) {
    this.module = module;
  }

  write(keyframe: RandomizableKeyframe<number>, ptr: number): void {
    const dataView = this.module.wasm.HEAP_DATA_VIEW;
    dataView.setFloat32(ptr, keyframe.time, true);
    dataView.setInt32(ptr + 4, keyframe.ease, true);
    dataView.setFloat32(ptr + 8, keyframe.value, true);
    dataView.setInt32(ptr + 12, keyframe.randomMode, true);
    dataView.setFloat32(ptr + 16, keyframe.randomValue, true);
    dataView.setInt32(ptr + 20, keyframe.randomInterval, true);
    dataView.setInt32(ptr + 24, keyframe.isRelative ? 1 : 0, true);
  }

  read(ptr: number): RandomizableKeyframe<number> {
    const dataView = this.module.wasm.HEAP_DATA_VIEW;
    const time = dataView.getFloat32(ptr, true);
    const ease = dataView.getInt32(ptr + 4, true);
    const value = dataView.getFloat32(ptr + 8, true);
    const randomMode = dataView.getInt32(ptr + 12, true);
    const randomValue = dataView.getFloat32(ptr + 16, true);
    const randomInterval = dataView.getInt32(ptr + 20, true);
    const isRelative = dataView.getInt32(ptr + 24, true) !== 0;

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

export class KeyframeBeatmapObjectIndexedColorAdapter implements KeyframeAdapter<Keyframe<BeatmapObjectIndexedColor>> {
  size: number = 20;

  private readonly module: Module;

  public constructor(module: Module) {
    this.module = module;
  }

  write(keyframe: Keyframe<BeatmapObjectIndexedColor>, ptr: number): void {
    const dataView = this.module.wasm.HEAP_DATA_VIEW;
    dataView.setFloat32(ptr, keyframe.time, true);
    dataView.setInt32(ptr + 4, keyframe.ease, true);
    dataView.setInt32(ptr + 8, keyframe.value.colorIndex1, true);
    dataView.setInt32(ptr + 12, keyframe.value.colorIndex2, true);
    dataView.setFloat32(ptr + 16, keyframe.value.opacity, true);
  }

  read(ptr: number): Keyframe<BeatmapObjectIndexedColor> {
    const dataView = this.module.wasm.HEAP_DATA_VIEW;
    const time = dataView.getFloat32(ptr, true);
    const ease = dataView.getInt32(ptr + 4, true);
    const colorIndex1 = dataView.getInt32(ptr + 8, true);
    const colorIndex2 = dataView.getInt32(ptr + 12, true);
    const opacity = dataView.getFloat32(ptr + 16, true);

    return {
      time,
      ease,
      value: {
        colorIndex1,
        colorIndex2,
        opacity
      }
    };
  }
}