import { PASWasmModule } from "./PASModule";

export class PASApp {
  private module: PASWasmModule;
  private ptr: number;

  public constructor(module: PASWasmModule, ptr: number) {
    this.module = module;
    this.ptr = ptr;
  }

  processFrame(time: number): void {
    this.module._app_processFrame(this.ptr, time);
  }

  release(): void {
    this.module._main_releaseAppPointer(this.ptr);
  }
}