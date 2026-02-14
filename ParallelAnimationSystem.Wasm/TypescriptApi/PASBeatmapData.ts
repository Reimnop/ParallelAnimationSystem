import { PASModule } from "./PASModule";
import { PASNativeObject } from "./PASNativeObject";

export class PASBeatmapData implements PASNativeObject {
  ptr: number;
  
  private readonly module: PASModule;
  
  public constructor(ptr: number, module: PASModule) {
    this.ptr = ptr;
    this.module = module;
    
    this.module.memoryManager.register(this);
  }
  
  release(): void {
    this.module.memoryManager.release(this);
  }
}