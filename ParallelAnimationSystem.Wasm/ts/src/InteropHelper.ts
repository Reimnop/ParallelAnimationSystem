import type { WasmModule } from "./Module";

export class InteropHelper {
  private readonly wasm: WasmModule;
  
  public constructor(wasm: WasmModule) {
    this.wasm = wasm;
  }
  
  stringToUTF8OnStack(str: string): number {
    const length = this.wasm.lengthBytesUTF8(str) + 1;
    const ptr = this.wasm.stackAlloc(length);
    this.wasm.stringToUTF8(str, ptr, length);
    return ptr;
  }
  
  stringToUTF8OnHeap(str: string): number {
    const length = this.wasm.lengthBytesUTF8(str) + 1;
    const ptr = this.wasm._interop_alloc(length);
    try {
      this.wasm.stringToUTF8(str, ptr, length);
      return ptr;
    } catch(e) {
      this.wasm._interop_free(ptr);
      throw e;
    }
  }
  
  stringPtrToString(ptr: number): string {
    const sp = this.wasm.stackSave();
    try {
      const size = this.wasm._string_getByteCount(ptr);
      const buf = this.wasm.stackAlloc(size + 1);
      this.wasm._string_getBytes(ptr, buf, size);
      this.wasm.HEAP_DATA_VIEW.setUint8(buf + size, 0); // null-terminate
      return this.wasm.UTF8ToString(buf);
    } finally {
      this.wasm.stackRestore(sp);
    }
  }
  
  getStringFromObject(ptr: number, getStringPtr: (ptr: number) => number): string | null {
    const stringPtr = getStringPtr(ptr);
    if (stringPtr === 0) {
      return null;
    }
    try {
      return this.stringPtrToString(stringPtr);
    } finally {
      this.wasm._interop_releasePointer(stringPtr);
    }
  }
  
  setStringToObject(ptr: number, value: string | null, setStringPtr: (ptr: number, strPtr: number) => void): void {
    if (value === null) {
      setStringPtr(ptr, 0);
      return;
    }
    const sp = this.wasm.stackSave();
    try {
      const valuePtr = this.stringToUTF8OnStack(value);
      setStringPtr(ptr, valuePtr);
    } finally {
      this.wasm.stackRestore(sp);
    }
  }
  
  getStringFromObjectNotNull(ptr: number, getStringPtr: (ptr: number) => number): string {
    const str = this.getStringFromObject(ptr, getStringPtr);
    if (str === null) {
      throw new Error("Expected non-null string");
    }
    return str;
  }
}
