import { Module } from "./Module";

export type NativeObjectConstructor<T extends NativeObject> = new (ptr: number, module: Module) => T;

export abstract class NativeObject {
  ptr: number;
  module: Module;
  
  public constructor(ptr: number, module: Module) {
    this.ptr = ptr;
    this.module = module;
    
    this.module.memoryManager.register(this);
  }
  
  release(): void {
    this.module.memoryManager.release(this);
  }
  
  equals(other: NativeObject): boolean {
    return this.ptr === other.ptr;
  }
}
