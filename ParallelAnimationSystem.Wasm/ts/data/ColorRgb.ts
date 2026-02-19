export type ColorRgb = number[] & { length: 3 };

export namespace ColorRgbs {
  export const White = hexToColorRgb("#FFFFFF");
  export const Red = hexToColorRgb("#FF0000");
  export const Green = hexToColorRgb("#00FF00");
  export const Blue = hexToColorRgb("#0000FF");
  export const Yellow = hexToColorRgb("#FFFF00");
  export const Cyan = hexToColorRgb("#00FFFF");
  export const Magenta = hexToColorRgb("#FF00FF");
  export const Black = hexToColorRgb("#000000");
}

export function createColorRgb(r: number, g: number, b: number): ColorRgb {
  return [r, g, b];
}

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