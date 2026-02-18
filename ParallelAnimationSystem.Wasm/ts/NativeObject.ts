import { Module } from "./Module";

export type NativeObjectConstructor<T extends NativeObject> = new (module: Module, ptr: number) => T;

export abstract class NativeObject {
  module: Module;
  ptr: number;
  
  public constructor(module: Module, ptr: number) {
    this.module = module;
    this.ptr = ptr;
    
    this.module.memoryManager.register(this);
  }
  
  release(): void {
    this.module.memoryManager.release(this);
  }
  
  equals(other: NativeObject): boolean {
    return this.ptr === other.ptr;
  }
}
