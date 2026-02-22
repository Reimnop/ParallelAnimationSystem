import type { Vector } from "./Vector";

export interface VignetteData {
  intensity: number;
  smoothness: number;
  color: number | null;
  rounded: boolean;
  roundness: number | null;
  center: Vector<2>;
}