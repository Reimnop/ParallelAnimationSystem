import type { Vector } from "../data/Vector";
import type { BeatmapObjectIndexedColor } from "../data/BeatmapObjectIndexedColor";

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
  