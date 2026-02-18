import type { Module } from "../Module";
import type { Keyframe, RandomizableKeyframe } from "../data/Keyframe";
import type { BeatmapObjectIndexedColor } from "../data/BeatmapObjectIndexedColor";
import type { Ease } from "../data/Ease";
import type { Vector } from "../data/Vector";
import type { RandomMode } from "../data/RandomMode";

export function serializeBeatmapObjectIndexedColorKeyframe(
  module: Module,
  keyframe: Keyframe<BeatmapObjectIndexedColor>): number {
  const { time, ease, value } = keyframe;
  const valuePtr = module.wasm._interop_alloc(12); // int, int, float
  try {
    module.wasm.HEAP32[valuePtr >> 2] = value.colorIndex1;
    module.wasm.HEAP32[(valuePtr >> 2) + 1] = value.colorIndex2;
    module.wasm.HEAPF32[(valuePtr >> 2) + 2] = value.opacity;
    return module.wasm._keyframe_beatmapObjectIndexedColor_new(time, ease, valuePtr);
  } finally {
    module.wasm._interop_free(valuePtr);
  }
}

export function deserializeBeatmapObjectIndexedColorKeyframe(
  module: Module,
  ptr: number): Keyframe<BeatmapObjectIndexedColor> {
  const time = module.wasm._keyframe_getTime(ptr);
  const ease = module.wasm._keyframe_getEase(ptr) as Ease;
  const valuePtr = module.wasm._interop_alloc(12); // int, int, float
  try {
    module.wasm._keyframe_beatmapObjectIndexedColor_getValue(ptr, valuePtr);
    const value: BeatmapObjectIndexedColor = {
      colorIndex1: module.wasm.HEAP32[valuePtr >> 2],
      colorIndex2: module.wasm.HEAP32[(valuePtr >> 2) + 1],
      opacity: module.wasm.HEAPF32[(valuePtr >> 2) + 2]
    };
    return { time, ease, value };
  } finally {
    module.wasm._interop_free(valuePtr);
  }
}

export function serializeVector2RandomizableKeyframe(module: Module, keyframe: RandomizableKeyframe<Vector<2>>): number {
  const valuesPtr = module.wasm._interop_alloc(16); // 4 floats
  try {
    module.wasm.HEAPF32[valuesPtr >> 2] = keyframe.value[0];
    module.wasm.HEAPF32[(valuesPtr >> 2) + 1] = keyframe.value[1];
    module.wasm.HEAPF32[(valuesPtr >> 2) + 2] = keyframe.randomValue[0];
    module.wasm.HEAPF32[(valuesPtr >> 2) + 3] = keyframe.randomValue[1];
    return module.wasm._randomizableKeyframe_vector2_new(
      keyframe.time, keyframe.ease, valuesPtr,
      keyframe.randomMode, valuesPtr + 8, keyframe.randomInterval,
      keyframe.isRelative ? 1 : 0);
  } finally {
    module.wasm._interop_free(valuesPtr);
  }
}

export function deserializeVector2RandomizableKeyframe(
  module: Module,
  ptr: number): RandomizableKeyframe<Vector<2>> {
  const time = module.wasm._keyframe_getTime(ptr);
  const ease = module.wasm._keyframe_getEase(ptr) as Ease;
  const randomMode = module.wasm._randomizableKeyframe_vector2_getRandomMode(ptr) as RandomMode;
  const randomInterval = module.wasm._randomizableKeyframe_vector2_getRandomInterval(ptr);
  const isRelative = module.wasm._randomizableKeyframe_vector2_getIsRelative(ptr) !== 0;
  const valuesPtr = module.wasm._interop_alloc(16); // 4 floats
  try {
    module.wasm._randomizableKeyframe_vector2_getValue(ptr, valuesPtr);
    module.wasm._randomizableKeyframe_vector2_getRandomValue(ptr, valuesPtr + 8);
    
    const value: Vector<2> = [
      module.wasm.HEAPF32[valuesPtr >> 2],
      module.wasm.HEAPF32[(valuesPtr >> 2) + 1]
    ];
    
    const randomValue: Vector<2> = [
      module.wasm.HEAPF32[(valuesPtr >> 2) + 2],
      module.wasm.HEAPF32[(valuesPtr >> 2) + 3]
    ]
    
    return {
      time, ease, value,
      randomMode, randomValue, randomInterval,
      isRelative
    };
  } finally {
    module.wasm._interop_free(valuesPtr);
  }
}

export function serializeFloatRandomizableKeyframe(module: Module, keyframe: RandomizableKeyframe<number>): number {
  return module.wasm._randomizableKeyframe_float_new(
    keyframe.time, keyframe.ease, keyframe.value,
    keyframe.randomMode, keyframe.randomValue, keyframe.randomInterval,
    keyframe.isRelative ? 1 : 0);
}

export function deserializeFloatRandomizableKeyframe(
  module: Module,
  ptr: number): RandomizableKeyframe<number> {
  const time = module.wasm._keyframe_getTime(ptr);
  const ease = module.wasm._keyframe_getEase(ptr) as Ease;
  const value = module.wasm._randomizableKeyframe_float_getValue(ptr);
  const randomMode = module.wasm._randomizableKeyframe_float_getRandomMode(ptr) as RandomMode;
  const randomValue = module.wasm._randomizableKeyframe_float_getRandomValue(ptr);
  const randomInterval = module.wasm._randomizableKeyframe_float_getRandomInterval(ptr);
  const isRelative = module.wasm._randomizableKeyframe_float_getIsRelative(ptr) !== 0;
  return {
    time, ease, value,
    randomMode, randomValue, randomInterval,
    isRelative
  };
}