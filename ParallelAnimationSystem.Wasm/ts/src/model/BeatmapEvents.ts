import { NativeObject } from "../NativeObject";
import type { KeyframeList } from "./KeyframeList";
import type { Vector } from "../data/Vector";
import { FixedSizeKeyframeList } from "./FixedSizeKeyframeList";
import { KeyframeCodec } from "./FixedSizeKeyframeCodec";
import {
  BloomDataCodec,
  Float32Codec, GlitchDataCodec, GradientDataCodec,
  GrainDataCodec,
  LensDistortionDataCodec,
  Vector2Codec,
  VignetteDataCodec
} from "./StructCodec";
import type { Keyframe } from "../data/Keyframe";
import { DynamicSizeKeyframeList } from "./DynamicSizeKeyframeList";
import { StringKeyframeCodec } from "./DynamicSizeKeyframeCodec";
import type { BloomData } from "../data/BloomData";
import type { LensDistortionData } from "../data/LensDistortionData";
import type { VignetteData } from "../data/VignetteData";
import type { GrainData } from "../data/GrainData";
import type { GradientData } from "../data/GradientData";
import type { GlitchData } from "../data/GlitchData";

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

  get chroma(): KeyframeList<Keyframe<number>> {
    const ptr = this.wasm._beatmapEvents_getChroma(this.ptr);
    return new FixedSizeKeyframeList<Keyframe<number>>(this.module, ptr,
      new KeyframeCodec<number>(this.module, new Float32Codec()));
  }

  get bloom(): KeyframeList<Keyframe<BloomData>> {
    const ptr = this.wasm._beatmapEvents_getBloom(this.ptr);
    return new FixedSizeKeyframeList<Keyframe<BloomData>>(this.module, ptr,
      new KeyframeCodec<BloomData>(this.module, new BloomDataCodec()));
  }

  get vignette(): KeyframeList<Keyframe<VignetteData>> {
    const ptr = this.wasm._beatmapEvents_getVignette(this.ptr);
    return new FixedSizeKeyframeList<Keyframe<VignetteData>>(this.module, ptr,
      new KeyframeCodec<VignetteData>(this.module, new VignetteDataCodec()));
  }

  get lensDistortion(): KeyframeList<Keyframe<LensDistortionData>> {
    const ptr = this.wasm._beatmapEvents_getLensDistortion(this.ptr);
    return new FixedSizeKeyframeList<Keyframe<LensDistortionData>>(this.module, ptr,
      new KeyframeCodec<LensDistortionData>(this.module, new LensDistortionDataCodec()));
  }

  get grain(): KeyframeList<Keyframe<GrainData>> {
    const ptr = this.wasm._beatmapEvents_getGrain(this.ptr);
    return new FixedSizeKeyframeList<Keyframe<GrainData>>(this.module, ptr,
      new KeyframeCodec<GrainData>(this.module, new GrainDataCodec()));
  }
  
  get gradient(): KeyframeList<Keyframe<GradientData>> {
    const ptr = this.wasm._beatmapEvents_getGradient(this.ptr);
    return new FixedSizeKeyframeList<Keyframe<GradientData>>(this.module, ptr,
      new KeyframeCodec<GradientData>(this.module, new GradientDataCodec()));
  }
  
  get glitch(): KeyframeList<Keyframe<GlitchData>> {
    const ptr = this.wasm._beatmapEvents_getGlitch(this.ptr);
    return new FixedSizeKeyframeList<Keyframe<GlitchData>>(this.module, ptr,
      new KeyframeCodec<GlitchData>(this.module, new GlitchDataCodec()));
  }
  
  get hue(): KeyframeList<Keyframe<number>> {
    const ptr = this.wasm._beatmapEvents_getHue(this.ptr);
    return new FixedSizeKeyframeList<Keyframe<number>>(this.module, ptr,
      new KeyframeCodec<number>(this.module, new Float32Codec()));
  }
}