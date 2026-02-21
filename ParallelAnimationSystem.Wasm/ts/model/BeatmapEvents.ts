import { NativeObject } from "../NativeObject";
import type { KeyframeList } from "./KeyframeList";
import type { Vector } from "../data/Vector";
import { FixedSizeKeyframeList } from "./FixedSizeKeyframeList";
import { KeyframeCodec } from "./FixedSizeKeyframeCodec";
import { Float32Codec, Vector2Codec } from "./StructCodec";
import type { Keyframe } from "../data/Keyframe";
import { DynamicSizeKeyframeList } from "./DynamicSizeKeyframeList";
import { StringKeyframeCodec } from "./DynamicSizeKeyframeCodec";

export class BeatmapEvents extends NativeObject {
  get cameraPosition(): KeyframeList<Keyframe<Vector<2>>> {
    const ptr = this.wasm._beatmapEvents_getCameraPosition(this.ptr);
    return new FixedSizeKeyframeList<Keyframe<Vector<2>>>(this.module, ptr,
      new KeyframeCodec<Vector<2>>(this.module, new Vector2Codec()));
  }
  
  get cameraScale(): KeyframeList<Keyframe<number>> {
    const ptr = this.wasm._beatmapEvents_getCameraScale(this.ptr);
    return new FixedSizeKeyframeList<Keyframe<number>>(this.module, ptr,
      new KeyframeCodec<number>(this.module, new Float32Codec()));
  }
  
  get cameraRotation(): KeyframeList<Keyframe<number>> {
    const ptr = this.wasm._beatmapEvents_getCameraRotation(this.ptr);
    return new FixedSizeKeyframeList<Keyframe<number>>(this.module, ptr,
      new KeyframeCodec<number>(this.module, new Float32Codec()));
  }
  
  get cameraShake(): KeyframeList<Keyframe<number>> {
    const ptr = this.wasm._beatmapEvents_getCameraShake(this.ptr);
    return new FixedSizeKeyframeList<Keyframe<number>>(this.module, ptr,
      new KeyframeCodec<number>(this.module, new Float32Codec()));
  }
  
  get theme(): KeyframeList<Keyframe<string>> {
    const ptr = this.wasm._beatmapEvents_getTheme(this.ptr);
    return new DynamicSizeKeyframeList<Keyframe<string>>(this.module, ptr,
      new StringKeyframeCodec(this.module));
  }
}