import type { Ease } from "./Ease";
import type { RandomMode } from "./RandomMode";

export interface Keyframe<T> {
  time: number;
  ease: Ease;
  value: T;
}

export interface RandomizableKeyframe<T> extends Keyframe<T> {
  randomMode: RandomMode;
  randomValue: T;
  randomInterval: number;
  isRelative: boolean;
}