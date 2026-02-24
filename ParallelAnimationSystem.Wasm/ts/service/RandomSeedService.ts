import { NativeObject } from "../NativeObject";

export class RandomSeedService extends NativeObject {
  get seed(): BigInt {
    return this.wasm._randomSeedService_getSeed(this.ptr);
  }
  
  set seed(value: BigInt) {
    this.wasm._randomSeedService_setSeed(this.ptr, value);
  }
}