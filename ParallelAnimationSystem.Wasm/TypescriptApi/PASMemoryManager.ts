import type { PASNativeObject } from "./PASNativeObject";
import type { PASWasmModule } from "./PASModule";

export class PASMemoryManager {
  private readonly finalizationRegistry: FinalizationRegistry<{ wasm: PASWasmModule, ptr: number }>;
  private readonly wasm: PASWasmModule;
  
  public constructor(wasm: PASWasmModule) {
    this.wasm = wasm;
    this.finalizationRegistry = new FinalizationRegistry(({ wasm, ptr }) => {
      wasm._interop_releasePointer(ptr);
    });
  }
  
  public register(obj: PASNativeObject) {
    const ptr = obj.ptr;
    this.finalizationRegistry.register(obj, { wasm: this.wasm, ptr }, obj);
  }
  
  public release(obj: PASNativeObject) {
    if (obj.ptr === 0) {
      return;
    }
    
    this.wasm._interop_releasePointer(obj.ptr);
    this.finalizationRegistry.unregister(obj);
    obj.ptr = 0; // prevent double release
  }
}