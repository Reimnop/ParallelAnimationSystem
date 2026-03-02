export type Vector<N extends number> = N extends 0 
  ? never 
  : number[] & { length: N };