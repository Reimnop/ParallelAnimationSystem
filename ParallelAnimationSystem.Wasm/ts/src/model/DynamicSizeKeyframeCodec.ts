// codec for dynamic-size keyframe value types
import type { Module } from "../Module";
import type { Keyframe } from "../data/Keyframe";

export interface DynamicSizeKeyframeCodec<T> {
  getSize(value: T): number;
  write(value: T, ptr: number): number;
  read(ptr: number): { value: T, bytesRead: number };
}

export class StringKeyframeCodec implements DynamicSizeKeyframeCodec<Keyframe<string>> {
  private readonly module: Module;

  public constructor(module: Module) {
    this.module = module;
  }

  getSize(value: Keyframe<string>): number {
    return 4 + 4 + this.module.wasm.lengthBytesUTF8(value.value) + 1; // time + ease + string bytes + null terminator
  }

  write(keyframe: Keyframe<string>, ptr: number): number {
    const view = this.module.wasm.HEAP_DATA_VIEW;
    let p = ptr;

    view.setFloat32(p, keyframe.time, true);
    p += 4;

    view.setInt32(p, keyframe.ease, true);
    p += 4;

    const stringByteCount = this.module.wasm.lengthBytesUTF8(keyframe.value);
    this.module.wasm.stringToUTF8(keyframe.value, p, stringByteCount + 1);
    p += stringByteCount + 1;

    return 4 + 4 + stringByteCount + 1;
  }
  
  read(ptr: number): { value: Keyframe<string>, bytesRead: number } {
    const view = this.module.wasm.HEAP_DATA_VIEW;
    let p = ptr;

    const time = view.getFloat32(p, true);
    p += 4;

    const ease = view.getInt32(p, true);
    p += 4;

    const value = this.module.wasm.UTF8ToString(p);
    const stringByteCount = this.module.wasm.lengthBytesUTF8(value);
    p += stringByteCount + 1;

    return { 
      value: { time, ease, value }, 
      bytesRead: 4 + 4 + stringByteCount + 1 
    };
  }
}
