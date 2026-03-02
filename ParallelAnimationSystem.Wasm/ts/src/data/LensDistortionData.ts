import type { Vector } from "./Vector";

export interface LensDistortionData {
  intensity: number;
  center: Vector<2>;
}