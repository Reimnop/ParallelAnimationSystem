import { NativeObject } from "../NativeObject";
import type { ColorRgb } from "../data/ColorRgb";

export class BeatmapThemeColorList extends NativeObject {
  get count(): number {
    return this.wasm._beatmapThemeColorList_getCount(this.ptr);
  }
  
  fetchAt(index: number): ColorRgb {
    const sp = this.module.wasm.stackSave();
    try {
      const buf = this.module.wasm.stackAlloc(12);
      this.module.wasm._beatmapThemeColorList_fetchAt(this.ptr, index, buf);
      const r = this.module.wasm.HEAP_DATA_VIEW.getFloat32(buf, true);
      const g = this.module.wasm.HEAP_DATA_VIEW.getFloat32(buf + 4, true);
      const b = this.module.wasm.HEAP_DATA_VIEW.getFloat32(buf + 8, true);
      return [r, g, b];
    } finally {
      this.module.wasm.stackRestore(sp);
    }
  }
  
  setAt(index: number, value: ColorRgb) {
    const sp = this.module.wasm.stackSave();
    try {
      const buf = this.module.wasm.stackAlloc(12);
      this.module.wasm.HEAP_DATA_VIEW.setFloat32(buf, value[0], true);
      this.module.wasm.HEAP_DATA_VIEW.setFloat32(buf + 4, value[1], true);
      this.module.wasm.HEAP_DATA_VIEW.setFloat32(buf + 8, value[2], true);
      this.module.wasm._beatmapThemeColorList_setAt(this.ptr, index, buf);
    } finally {
      this.module.wasm.stackRestore(sp);
    }
  }
}