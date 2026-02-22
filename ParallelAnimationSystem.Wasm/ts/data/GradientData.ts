export enum GradientOverlayMode {
  Linear,
  Additive,
  Multiply,
  Screen
}

export interface GradientData {
  intensity: number;
  rotation: number;
  colorA: number;
  colorB: number;
  mode: GradientOverlayMode;
}