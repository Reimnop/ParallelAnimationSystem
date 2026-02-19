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
  const sp = module.wasm.stackSave();
  try {
    const valuePtr = module.wasm.stackAlloc(12); // int, int, float
    module.wasm.HEAP_DATA_VIEW.setInt32(valuePtr, value.colorIndex1, true);
    module.wasm.HEAP_DATA_VIEW.setInt32(valuePtr + 4, value.colorIndex2, true);
    module.wasm.HEAP_DATA_VIEW.setFloat32(valuePtr + 8, value.opacity, true);
    return module.wasm._keyframe_beatmapObjectIndexedColor_new(time, ease, valuePtr);
  } finally {
    module.wasm.stackRestore(sp);
  }
}

export function deserializeBeatmapObjectIndexedColorKeyframe(
  module: Module,
  ptr: number): Keyframe<BeatmapObjectIndexedColor> {
  const time = module.wasm._keyframe_getTime(ptr);
  const ease = module.wasm._keyframe_getEase(ptr) as Ease;
  const sp = module.wasm.stackSave();
  try {
    const valuePtr = module.wasm.stackAlloc(12); // int, int, float
    module.wasm._keyframe_beatmapObjectIndexedColor_getValue(ptr, valuePtr);
    const value: BeatmapObjectIndexedColor = {
      colorIndex1: module.wasm.HEAP_DATA_VIEW.getInt32(valuePtr, true),
      colorIndex2: module.wasm.HEAP_DATA_VIEW.getInt32(valuePtr + 4, true),
      opacity: module.wasm.HEAP_DATA_VIEW.getFloat32(valuePtr + 8, true)
    };
    return { time, ease, value };
  } finally {
    module.wasm.stackRestore(sp);
  }
}

export function serializeVector2RandomizableKeyframe(module: Module, keyframe: RandomizableKeyframe<Vector<2>>): number {
  const sp = module.wasm.stackSave();
  try {
    const valuesPtr = module.wasm.stackAlloc(8); // 2 floats for value
    module.wasm.HEAP_DATA_VIEW.setFloat32(valuesPtr, keyframe.value[0], true);
    module.wasm.HEAP_DATA_VIEW.setFloat32(valuesPtr + 4, keyframe.value[1], true);
    
    const randomValuesPtr = module.wasm.stackAlloc(8); // 2 floats for randomValue
    module.wasm.HEAP_DATA_VIEW.setFloat32(randomValuesPtr, keyframe.randomValue[0], true);
    module.wasm.HEAP_DATA_VIEW.setFloat32(randomValuesPtr + 4, keyframe.randomValue[1], true);
    
    return module.wasm._randomizableKeyframe_vector2_new(
      keyframe.time, keyframe.ease, valuesPtr,
      keyframe.randomMode, randomValuesPtr, keyframe.randomInterval,
      keyframe.isRelative ? 1 : 0);
  } finally {
    module.wasm.stackRestore(sp);
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
  const sp = module.wasm.stackSave();
  try {
    const valuesPtr = module.wasm.stackAlloc(8); // 2 floats for value
    module.wasm._randomizableKeyframe_vector2_getValue(ptr, valuesPtr);
    
    const randomValuesPtr = module.wasm.stackAlloc(8); // 2 floats for randomValue
    module.wasm._randomizableKeyframe_vector2_getRandomValue(ptr, randomValuesPtr);
    
    const value: Vector<2> = [
      module.wasm.HEAP_DATA_VIEW.getFloat32(valuesPtr, true),
      module.wasm.HEAP_DATA_VIEW.getFloat32(valuesPtr + 4, true)
    ];
    
    const randomValue: Vector<2> = [
      module.wasm.HEAP_DATA_VIEW.getFloat32(randomValuesPtr, true),
      module.wasm.HEAP_DATA_VIEW.getFloat32(randomValuesPtr + 4, true)
    ];
    
    return {
      time, ease, value,
      randomMode, randomValue, randomInterval,
      isRelative
    };
  } finally {
    module.wasm.stackRestore(sp);
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