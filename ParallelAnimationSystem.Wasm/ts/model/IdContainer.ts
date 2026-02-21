import { NativeObject, type NativeObjectConstructor } from "../NativeObject";
import { Module } from "../Module";

export class IdContainer<T extends NativeObject> extends NativeObject {
  
  private readonly ctor: NativeObjectConstructor<T>;
  
  constructor(module: Module, ptr: number, ctor: NativeObjectConstructor<T>) {
    super(module, ptr);
    this.ctor = ctor;
  }
  
  get count(): number {
    return this.wasm._idContainer_getCount(this.ptr);
  }
  
  insert(item: T): boolean {
    return this.wasm._idContainer_insert(this.ptr, item.ptr) !== 0;
  }
  
  remove(id: string): boolean {
    const sp = this.wasm.stackSave();
    try {
      const idPtr = this.interopHelper.stringToUTF8OnStack(id);
      return this.wasm._idContainer_remove(this.ptr, idPtr) !== 0;
    } finally {
      this.wasm.stackRestore(sp);
    }
  }
  
  getById(id: string): T | null {
    const sp = this.wasm.stackSave();
    try {
      const idPtr = this.interopHelper.stringToUTF8OnStack(id);
      const itemPtr = this.wasm._idContainer_getById(this.ptr, idPtr);
      if (itemPtr === 0) {
        return null;
      }
      return new this.ctor(this.module, itemPtr);
    } finally {
      this.wasm.stackRestore(sp);
    }
  }
  
  *[Symbol.iterator](): Iterator<[string, T]> {
    const iteratorPtr = this.wasm._idContainer_getIterator(this.ptr);
    try {
      while (this.wasm._idContainer_iterator_moveNext(iteratorPtr) !== 0) {
        const id = this.interopHelper.getStringFromObjectNotNull(iteratorPtr, this.wasm._idContainer_iterator_getCurrent_key);
        const itemPtr = this.wasm._idContainer_iterator_getCurrent_value(iteratorPtr);
        try {
          const item = new this.ctor(this.module, itemPtr);
          yield [id, item];
        } catch(e) {
          this.wasm._interop_releasePointer(itemPtr);
          throw e;
        }
      }
    } finally {
      this.wasm._idContainer_iterator_dispose(iteratorPtr);
      this.wasm._interop_releasePointer(iteratorPtr);
    }
  }
}