import { Module, type WasmModule } from "./Module";
import { InteropHelper } from "./InteropHelper";
import { MemoryManager } from "./MemoryManager";

export type NativeObjectConstructor<T extends NativeObject> = new (module: Module, ptr: number) => T;

export abstract class NativeObject {
  module: Module;
  ptr: number;
  
  protected wasm: WasmModule;
  protected memoryManager: MemoryManager;
  protected interopHelper: InteropHelper;
  
  public constructor(module: Module, ptr: number) {
    this.module = module;
    this.ptr = ptr;
    
    this.wasm = module.wasm;
    this.memoryManager = module.memoryManager;
    this.interopHelper = module.interopHelper;
    
    this.memoryManager.register(this);
  }
  
  release(): void {
    this.memoryManager.release(this);
  }
  
  equals(other: NativeObject): boolean {
    return this.module === other.module && this.ptr === other.ptr;
  }
}
