import type { Vector } from "./Vector";

export type ColorRgb = Vector<3>;

export const Colors = {
  white: hexToColorRgb("#FFFFFF"),
  red: hexToColorRgb("#FF0000"),
  green: hexToColorRgb("#00FF00"),
  blue: hexToColorRgb("#0000FF"),
  yellow: hexToColorRgb("#FFFF00"),
  cyan: hexToColorRgb("#00FFFF"),
  magenta: hexToColorRgb("#FF00FF"),
  black: hexToColorRgb("#000000")
};

export function createColorRgb255(r: number, g: number, b: number): ColorRgb {
  return [r / 255, g / 255, b / 255];
}

export function colorRgbToHex(color: ColorRgb): string {
  return `#${numberToHex(color[0])}${numberToHex(color[1])}${numberToHex(color[2])}`;
}

export function hexToColorRgb(hex: string): ColorRgb {
  if (hex.startsWith("#")) {
    hex = hex.slice(1);
  }
  if (hex.length !== 6) {
    throw new Error("Invalid hex color format");
  }
  const r = parseInt(hex.slice(0, 2), 16) / 255;
  const g = parseInt(hex.slice(2, 4), 16) / 255;
  const b = parseInt(hex.slice(4, 6), 16) / 255;
  return [r, g, b];
}

function numberToHex(n: number): string {
  return Math.min(255, Math.round(n * 255)).toString(16).padStart(2, "0");
}