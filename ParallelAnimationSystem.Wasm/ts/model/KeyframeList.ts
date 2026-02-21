export interface KeyframeList<T> {
  get count(): number;
  fetchAt(index: number): T;
  fetchRange(start: number, count: number): T[];
  load(keyframes: T[]): void;
  toArray(): T[];
  [Symbol.iterator](): Iterator<T>;
}