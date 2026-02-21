import { NativeObject } from "../NativeObject";
import type { Module } from "../Module";
import type { ColorRgb } from "../data/ColorRgb";
import { BeatmapThemeColorList } from "./BeatmapThemeColorList";

export class BeatmapTheme extends NativeObject {
  static create(module: Module, id: string) {
    const sp = module.wasm.stackSave();
    try {
      const idPtr = module.interopHelper.stringToUTF8OnStack(id);
      const ptr = module.wasm._beatmapTheme_new(idPtr);
      return new BeatmapTheme(module, ptr);
    } finally {
      module.wasm.stackRestore(sp);
    }
  }
  
  get id(): string {
    return this.interopHelper.getStringFromObjectNotNull(this.ptr, this.wasm._beatmapTheme_getId);
  }
  
  get name(): string {
    return this.interopHelper.getStringFromObjectNotNull(this.ptr, this.wasm._beatmapTheme_getName);
  }
  
  set name(value: string) {
    this.interopHelper.setStringToObject(this.ptr, value, this.wasm._beatmapTheme_setName);
  }
  
  get backgroundColor(): ColorRgb {
    const sp = this.wasm.stackSave();
    try {
      const buf = this.wasm.stackAlloc(12);
      this.wasm._beatmapTheme_getBackgroundColor(this.ptr, buf);
      const r = this.wasm.HEAP_DATA_VIEW.getFloat32(buf, true);
      const g = this.wasm.HEAP_DATA_VIEW.getFloat32(buf + 4, true);
      const b = this.wasm.HEAP_DATA_VIEW.getFloat32(buf + 8, true);
      return [r, g, b];
    } finally {
      this.wasm.stackRestore(sp);
    }
  }
  
  set backgroundColor(value: ColorRgb) {
    const sp = this.wasm.stackSave();
    try {
      const buf = this.wasm.stackAlloc(12);
      this.wasm.HEAP_DATA_VIEW.setFloat32(buf, value[0], true);
      this.wasm.HEAP_DATA_VIEW.setFloat32(buf + 4, value[1], true);
      this.wasm.HEAP_DATA_VIEW.setFloat32(buf + 8, value[2], true);
      this.wasm._beatmapTheme_setBackgroundColor(this.ptr, buf);
    } finally {
      this.wasm.stackRestore(sp);
    }
  }
  
  get guiColor(): ColorRgb {
    const sp = this.wasm.stackSave();
    try {
      const buf = this.wasm.stackAlloc(12);
      this.wasm._beatmapTheme_getGuiColor(this.ptr, buf);
      const r = this.wasm.HEAP_DATA_VIEW.getFloat32(buf, true);
      const g = this.wasm.HEAP_DATA_VIEW.getFloat32(buf + 4, true);
      const b = this.wasm.HEAP_DATA_VIEW.getFloat32(buf + 8, true);
      return [r, g, b];
    } finally {
      this.wasm.stackRestore(sp);
    }
  }
  
  set guiColor(value: ColorRgb) {
    const sp = this.wasm.stackSave();
    try {
      const buf = this.wasm.stackAlloc(12);
      this.wasm.HEAP_DATA_VIEW.setFloat32(buf, value[0], true);
      this.wasm.HEAP_DATA_VIEW.setFloat32(buf + 4, value[1], true);
      this.wasm.HEAP_DATA_VIEW.setFloat32(buf + 8, value[2], true);
      this.wasm._beatmapTheme_setGuiColor(this.ptr, buf);
    } finally {
      this.wasm.stackRestore(sp);
    }
  }
  
  get guiAccentColor(): ColorRgb {
    const sp = this.wasm.stackSave();
    try {
      const buf = this.wasm.stackAlloc(12);
      this.wasm._beatmapTheme_getGuiAccentColor(this.ptr, buf);
      const r = this.wasm.HEAP_DATA_VIEW.getFloat32(buf, true);
      const g = this.wasm.HEAP_DATA_VIEW.getFloat32(buf + 4, true);
      const b = this.wasm.HEAP_DATA_VIEW.getFloat32(buf + 8, true);
      return [r, g, b];
    } finally {
      this.wasm.stackRestore(sp);
    }
  }
  
  set guiAccentColor(value: ColorRgb) {
    const sp = this.wasm.stackSave();
    try {
      const buf = this.wasm.stackAlloc(12);
      this.wasm.HEAP_DATA_VIEW.setFloat32(buf, value[0], true);
      this.wasm.HEAP_DATA_VIEW.setFloat32(buf + 4, value[1], true);
      this.wasm.HEAP_DATA_VIEW.setFloat32(buf + 8, value[2], true);
      this.wasm._beatmapTheme_setGuiAccentColor(this.ptr, buf);
    } finally {
      this.wasm.stackRestore(sp);
    }
  }
  
  get playerColors(): BeatmapThemeColorList {
    const ptr = this.wasm._beatmapTheme_getPlayerColors(this.ptr);
    return new BeatmapThemeColorList(this.module, ptr);
  }
  
  get objectColors(): BeatmapThemeColorList {
    const ptr = this.wasm._beatmapTheme_getObjectColors(this.ptr);
    return new BeatmapThemeColorList(this.module, ptr);
  }
  
  get effectColors(): BeatmapThemeColorList {
    const ptr = this.wasm._beatmapTheme_getEffectColors(this.ptr);
    return new BeatmapThemeColorList(this.module, ptr);
  }
  
  get parallaxObjectColors(): BeatmapThemeColorList {
    const ptr = this.wasm._beatmapTheme_getParallaxObjectColors(this.ptr);
    return new BeatmapThemeColorList(this.module, ptr);
  }
}