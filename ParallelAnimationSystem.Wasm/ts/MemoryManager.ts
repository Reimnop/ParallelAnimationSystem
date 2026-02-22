import type { NativeObject } from "./NativeObject";
import type { WasmModule } from "./Module";

export class MemoryManager {
  private readonly finalizationRegistry: FinalizationRegistry<{ wasm: WasmModule, ptr: number }>;
  private readonly wasm: WasmModule;
  
  public constructor(wasm: WasmModule) {
    this.wasm = wasm;
    this.finalizationRegistry = new FinalizationRegistry(({ wasm, ptr }) => {
      wasm._interop_releasePointer(ptr);
    });
  }
  
  public register(obj: NativeObject) {
    const ptr = obj.ptr;
    if (ptr === 0) {
      throw new Error("Cannot register native object with null pointer");
    }
    
    this.finalizationRegistry.register(obj, { wasm: this.wasm, ptr }, obj);
  }
  
  public release(obj: NativeObject) {
    if (obj.ptr === 0) {
      return;
    }
    
    this.wasm._interop_releasePointer(obj.ptr);
    this.finalizationRegistry.unregister(obj);
    obj.ptr = 0; // prevent double release
  }
}