import { NativeObject } from "../NativeObject";
import type { Module } from "../Module";
import type { BeatmapObjectType } from "../data/BeatmapObjectType";
import type { BeatmapObjectParentType } from "../data/BeatmapObjectParentType";
import type { BeatmapObjectParentOffset } from "../data/BeatmapObjectParentOffset";
import type { BeatmapObjectRenderType } from "../data/BeatmapObjectRenderType";
import type { Vector } from "../data/Vector";
import type { BeatmapObjectAutoKillType } from "../data/BeatmapObjectAutoKillType";
import type { BeatmapObjectShape } from "../data/BeatmapObjectShape";
import { KeyframeList } from "./KeyframeList";
import type { Keyframe, RandomizableKeyframe } from "../data/Keyframe";
import {
  deserializeBeatmapObjectIndexedColorKeyframe,
  deserializeFloatRandomizableKeyframe,
  deserializeVector2RandomizableKeyframe, serializeBeatmapObjectIndexedColorKeyframe,
  serializeFloatRandomizableKeyframe, serializeVector2RandomizableKeyframe
} from "./KeyframeHelper";
import type { BeatmapObjectIndexedColor } from "../data/BeatmapObjectIndexedColor";

export class BeatmapObject extends NativeObject {
  static create(module: Module, id: string): BeatmapObject {
    const sp = module.wasm.stackSave();
    try {
      const idPtr = module.interopHelper.stringToUTF8OnStack(id);
      const ptr = module.wasm._beatmapObject_new(idPtr);
      return new BeatmapObject(module, ptr);
    } finally {
      module.wasm.stackRestore(sp);
    }
  }
  
  get id(): string {
    return this.interopHelper.getStringFromObjectNotNull(this.ptr, this.wasm._beatmapObject_getId);
  }
  
  get name(): string {
    return this.interopHelper.getStringFromObjectNotNull(this.ptr, this.wasm._beatmapObject_getName);
  }
  
  set name(value: string) {
    this.interopHelper.setStringToObject(this.ptr, value, this.wasm._beatmapObject_setName);
  }
  
  get parentId(): string | null {
    return this.interopHelper.getStringFromObject(this.ptr, this.wasm._beatmapObject_getParentId);
  }
  
  set parentId(value: string | null) {
    this.interopHelper.setStringToObject(this.ptr, value, this.wasm._beatmapObject_setParentId);
  }
  
  get type(): BeatmapObjectType {
    return this.wasm._beatmapObject_getType(this.ptr);
  }
  
  set type(value: BeatmapObjectType) {
    this.wasm._beatmapObject_setType(this.ptr, value);
  }
  
  get parentType(): BeatmapObjectParentType {
    return this.wasm._beatmapObject_getParentType(this.ptr);
  }
  
  set parentType(value: BeatmapObjectParentType) {
    this.wasm._beatmapObject_setParentType(this.ptr, value);
  }
  
  get parentOffset(): BeatmapObjectParentOffset {
    const sp = this.wasm.stackSave();
    try {
      const buf = this.wasm.stackAlloc(12); // allocate buffer for 3 f32 values
      this.wasm._beatmapObject_getParentOffset(this.ptr, buf);
      const position = this.wasm.HEAP_DATA_VIEW.getFloat32(buf, true);
      const scale = this.wasm.HEAP_DATA_VIEW.getFloat32(buf + 4, true);
      const rotation = this.wasm.HEAP_DATA_VIEW.getFloat32(buf + 8, true);
      return { position, scale, rotation };
    } finally {
      this.wasm.stackRestore(sp);
    }
  }
  
  set parentOffset(value: BeatmapObjectParentOffset) {
    const sp = this.wasm.stackSave();
    try {
      const buf = this.wasm.stackAlloc(12); // allocate buffer for 3 f32 values
      this.wasm.HEAP_DATA_VIEW.setFloat32(buf, value.position, true);
      this.wasm.HEAP_DATA_VIEW.setFloat32(buf + 4, value.scale, true);
      this.wasm.HEAP_DATA_VIEW.setFloat32(buf + 8, value.rotation, true);
      this.wasm._beatmapObject_setParentOffset(this.ptr, buf);
    } finally {
      this.wasm.stackRestore(sp);
    }
  }
  
  get renderType(): BeatmapObjectRenderType {
    return this.wasm._beatmapObject_getRenderType(this.ptr);
  }
  
  set renderType(value: BeatmapObjectRenderType) {
    this.wasm._beatmapObject_setRenderType(this.ptr, value);
  }
  
  get origin(): Vector<2> {
    const sp = this.wasm.stackSave();
    try {
      const buf = this.wasm.stackAlloc(8); // allocate buffer for 2 f32 values
      this.wasm._beatmapObject_getOrigin(this.ptr, buf);
      const x = this.wasm.HEAP_DATA_VIEW.getFloat32(buf, true);
      const y = this.wasm.HEAP_DATA_VIEW.getFloat32(buf + 4, true);
      return [x, y];
    } finally {
      this.wasm.stackRestore(sp);
    }
  }
  
  set origin(value: Vector<2>) {
    const sp = this.wasm.stackSave();
    try {
      const buf = this.wasm.stackAlloc(8); // allocate buffer for 2 f32 values
      this.wasm.HEAP_DATA_VIEW.setFloat32(buf, value[0], true);
      this.wasm.HEAP_DATA_VIEW.setFloat32(buf + 4, value[1], true);
      this.wasm._beatmapObject_setOrigin(this.ptr, buf);
    } finally {
      this.wasm.stackRestore(sp);
    }
  }
  
  get renderDepth(): number {
    return this.wasm._beatmapObject_getRenderDepth(this.ptr);
  }
  
  set renderDepth(value: number) {
    this.wasm._beatmapObject_setRenderDepth(this.ptr, value);
  }
  
  get startTime(): number {
    return this.wasm._beatmapObject_getStartTime(this.ptr);
  }
  
  set startTime(value: number) {
    this.wasm._beatmapObject_setStartTime(this.ptr, value);
  }
  
  get autoKillType(): BeatmapObjectAutoKillType {
    return this.wasm._beatmapObject_getAutoKillType(this.ptr);
  }
  
  set autoKillType(value: BeatmapObjectAutoKillType) {
    this.wasm._beatmapObject_setAutoKillType(this.ptr, value);
  }
  
  get autoKillOffset(): number {
    return this.wasm._beatmapObject_getAutoKillOffset(this.ptr);
  }
  
  set autoKillOffset(value: number) {
    this.wasm._beatmapObject_setAutoKillOffset(this.ptr, value);
  }
  
  get shape(): BeatmapObjectShape {
    return this.wasm._beatmapObject_getShape(this.ptr);
  }
  
  set shape(value: BeatmapObjectShape) {
    this.wasm._beatmapObject_setShape(this.ptr, value);
  }
  
  get text(): string | null {
    return this.interopHelper.getStringFromObject(this.ptr, this.wasm._beatmapObject_getText);
  }
  
  set text(value: string | null) {
    this.interopHelper.setStringToObject(this.ptr, value, this.wasm._beatmapObject_setText);
  }
  
  get positionKeyframes(): KeyframeList<Vector<2>, RandomizableKeyframe<Vector<2>>> {
    const positionKeyframesPtr = this.wasm._beatmapObject_getPositionKeyframes(this.ptr);
    return new KeyframeList(
      serializeVector2RandomizableKeyframe,
      deserializeVector2RandomizableKeyframe,
      this.module, positionKeyframesPtr);
  }

  get scaleKeyframes(): KeyframeList<Vector<2>, RandomizableKeyframe<Vector<2>>> {
    const scaleKeyframesPtr = this.wasm._beatmapObject_getScaleKeyframes(this.ptr);
    return new KeyframeList(
      serializeVector2RandomizableKeyframe,
      deserializeVector2RandomizableKeyframe,
      this.module, scaleKeyframesPtr);
  }

  get rotationKeyframes(): KeyframeList<number, RandomizableKeyframe<number>> {
    const rotationKeyframesPtr = this.wasm._beatmapObject_getRotationKeyframes(this.ptr);
    return new KeyframeList(
      serializeFloatRandomizableKeyframe,
      deserializeFloatRandomizableKeyframe,
      this.module, rotationKeyframesPtr);
  }

  get colorKeyframes(): KeyframeList<BeatmapObjectIndexedColor, Keyframe<BeatmapObjectIndexedColor>> {
    const colorKeyframesPtr = this.wasm._beatmapObject_getColorKeyframes(this.ptr);
    return new KeyframeList(
      serializeBeatmapObjectIndexedColorKeyframe,
      deserializeBeatmapObjectIndexedColorKeyframe,
      this.module, colorKeyframesPtr);
  }
}