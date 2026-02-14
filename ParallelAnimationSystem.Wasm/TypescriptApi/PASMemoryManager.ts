import { PASNativeObject } from "./PASNativeObject";
import { PASWasmModule } from "./PASModule";

export class PASMemoryManager {
  private readonly finalizationRegistry: FinalizationRegistry<number>;
  private readonly wasm: PASWasmModule;
  
  public constructor(wasm: PASWasmModule) {
    this.wasm = wasm;
    this.finalizationRegistry = new FinalizationRegistry((ptr) => {
      this.wasm._interop_releasePointer(ptr);
    });
  }
  
  public register(obj: PASNativeObject) {
    this.finalizationRegistry.register(obj, obj.ptr);
  }
  
  public release(obj: PASNativeObject) {
    this.wasm._interop_releasePointer(obj.ptr);
    this.finalizationRegistry.unregister(obj);
  }
}