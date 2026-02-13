import { PASWasmModule } from "./PASModule";
import { NativeObject } from "./NativeObject";

export class PASBeatmapData implements NativeObject {
    ptr: number;
    
    private readonly module: PASWasmModule;
    
    public constructor(ptr: number, module: PASWasmModule) {
        this.ptr = ptr;
        this.module = module;
    }
}