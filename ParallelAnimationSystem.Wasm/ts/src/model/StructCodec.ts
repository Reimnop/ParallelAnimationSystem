import type { Vector } from "../data/Vector";
import type { BeatmapObjectIndexedColor } from "../data/BeatmapObjectIndexedColor";
import type { BloomData } from "../data/BloomData";
import type { VignetteData } from "../data/VignetteData";
import type { LensDistortionData } from "../data/LensDistortionData";
import type { GrainData } from "../data/GrainData";
import type { GradientData } from "../data/GradientData";
import type { GlitchData } from "../data/GlitchData";

export interface StructCodec<T> {
  size: number;
  write(dataView: DataView, value: T, ptr: number): void;
  read(dataView: DataView, ptr: number): T;
}

export class Vector2Codec implements StructCodec<Vector<2>> {
  size: number = 8;
  
  write(dataView: DataView, value: Vector<2>, ptr: number): void {
    dataView.setFloat32(ptr, value[0], true);
    dataView.setFloat32(ptr + 4, value[1], true);
  }
  
  read(dataView: DataView, ptr: number): Vector<2> {
    const x = dataView.getFloat32(ptr, true);
    const y = dataView.getFloat32(ptr + 4, true);
    return [x, y];
  }
}

export class Float32Codec implements StructCodec<number> {
  size: number = 4;
  
  write(dataView: DataView, value: number, ptr: number): void {
    dataView.setFloat32(ptr, value, true);
  }
  
  read(dataView: DataView, ptr: number): number {
    return dataView.getFloat32(ptr, true);
  }
}

export class BeatmapObjectIndexedColorCodec implements StructCodec<BeatmapObjectIndexedColor> {
  size: number = 12;
  
  write(dataView: DataView, value: BeatmapObjectIndexedColor, ptr: number): void {
    dataView.setInt32(ptr, value.colorIndex1, true);
    dataView.setInt32(ptr + 4, value.colorIndex2, true);
    dataView.setFloat32(ptr + 8, value.opacity, true);
  }
  
  read(dataView: DataView, ptr: number): BeatmapObjectIndexedColor {
    const colorIndex1 = dataView.getInt32(ptr, true);
    const colorIndex2 = dataView.getInt32(ptr + 4, true);
    const opacity = dataView.getFloat32(ptr + 8, true);
    return { colorIndex1, colorIndex2, opacity };
  }
}

export class BloomDataCodec implements StructCodec<BloomData> {
  size: number = 12;
  
  write(dataView: DataView, value: BloomData, ptr: number): void {
    dataView.setFloat32(ptr, value.intensity, true);
    dataView.setFloat32(ptr + 4, value.diffusion, true);
    dataView.setInt32(ptr + 8, value.color, true);
  }
  
  read(dataView: DataView, ptr: number): BloomData {
    const intensity = dataView.getFloat32(ptr, true);
    const diffusion = dataView.getFloat32(ptr + 4, true);
    const color = dataView.getInt32(ptr + 8, true);
    return { intensity, diffusion, color };
  }
}

export class VignetteDataCodec implements StructCodec<VignetteData> {
  size: number = 36;
  
  write(dataView: DataView, value: VignetteData, ptr: number): void {
    dataView.setFloat32(ptr, value.intensity, true);
    dataView.setFloat32(ptr + 4, value.smoothness, true);
    dataView.setUint8(ptr + 8, value.color !== null ? 1 : 0); // hasValue for color
    if (value.color !== null) {
      dataView.setInt32(ptr + 12, value.color, true);
    }
    dataView.setUint8(ptr + 16, value.rounded ? 1 : 0);
    // skip padding
    dataView.setUint8(ptr + 20, value.roundness !== null ? 1 : 0); // hasValue for roundness
    if (value.roundness !== null) {
      dataView.setFloat32(ptr + 24, value.roundness, true);
    }
    // center vector
    dataView.setFloat32(ptr + 28, value.center[0], true);
    dataView.setFloat32(ptr + 32, value.center[1], true);
  }
  
  read(dataView: DataView, ptr: number): VignetteData {
    const intensity = dataView.getFloat32(ptr, true);
    const smoothness = dataView.getFloat32(ptr + 4, true);
    const hasColor = dataView.getUint8(ptr + 8) !== 0;
    const color = hasColor ? dataView.getInt32(ptr + 12, true) : null;
    const rounded = dataView.getUint8(ptr + 16) !== 0;
    // skip padding
    const hasRoundness = dataView.getUint8(ptr + 20) !== 0;
    const roundness = hasRoundness ? dataView.getFloat32(ptr + 24, true) : null;
    const centerX = dataView.getFloat32(ptr + 28, true);
    const centerY = dataView.getFloat32(ptr + 32, true);

    return {
      intensity,
      smoothness,
      color,
      rounded,
      roundness,
      center: [centerX, centerY]
    };
  }
}

export class LensDistortionDataCodec implements StructCodec<LensDistortionData> {
  size: number = 12;
  
  write(dataView: DataView, value: LensDistortionData, ptr: number): void {
    dataView.setFloat32(ptr, value.intensity, true);
    dataView.setFloat32(ptr + 4, value.center[0], true);
    dataView.setFloat32(ptr + 8, value.center[1], true);
  }
  
  read(dataView: DataView, ptr: number): LensDistortionData {
    const intensity = dataView.getFloat32(ptr, true);
    const centerX = dataView.getFloat32(ptr + 4, true);
    const centerY = dataView.getFloat32(ptr + 8, true);
    return {
      intensity,
      center: [centerX, centerY]
    };
  }
}

export class GrainDataCodec implements StructCodec<GrainData> {
  size: number = 16;
  
  write(dataView: DataView, value: GrainData, ptr: number): void {
    dataView.setFloat32(ptr, value.intensity, true);
    dataView.setFloat32(ptr + 4, value.size, true);
    dataView.setFloat32(ptr + 8, value.mix, true);
    dataView.setUint8(ptr + 12, value.colored ? 1 : 0);
  }
  
  read(dataView: DataView, ptr: number): GrainData {
    const intensity = dataView.getFloat32(ptr, true);
    const size = dataView.getFloat32(ptr + 4, true);
    const mix = dataView.getFloat32(ptr + 8, true);
    const colored = dataView.getUint8(ptr + 12) !== 0;
    return {
      intensity,
      size,
      mix,
      colored
    };
  }
}

export class GradientDataCodec implements StructCodec<GradientData> {
  size: number = 20;
  
  write(dataView: DataView, value: GradientData, ptr: number): void {
    dataView.setFloat32(ptr, value.intensity, true);
    dataView.setFloat32(ptr + 4, value.rotation, true);
    dataView.setInt32(ptr + 8, value.colorA, true);
    dataView.setInt32(ptr + 12, value.colorB, true);
    dataView.setInt32(ptr + 16, value.mode, true);
  }
  
  read(dataView: DataView, ptr: number): GradientData {
    const intensity = dataView.getFloat32(ptr, true);
    const rotation = dataView.getFloat32(ptr + 4, true);
    const colorA = dataView.getInt32(ptr + 8, true);
    const colorB = dataView.getInt32(ptr + 12, true);
    const mode = dataView.getInt32(ptr + 16, true);
    
    return {
      intensity,
      rotation,
      colorA,
      colorB,
      mode
    };
  }
}

export class GlitchDataCodec implements StructCodec<GlitchData> {
  size: number = 12;
  
  write(dataView: DataView, value: GlitchData, ptr: number): void {
    dataView.setFloat32(ptr, value.intensity, true);
    dataView.setFloat32(ptr + 4, value.speed, true);
    dataView.setFloat32(ptr + 8, value.width, true);
  }
  
  read(dataView: DataView, ptr: number): GlitchData {
    const intensity = dataView.getFloat32(ptr, true);
    const speed = dataView.getFloat32(ptr + 4, true);
    const width = dataView.getFloat32(ptr + 8, true);
    return {
      intensity,
      speed,
      width
    };
  }
}
  