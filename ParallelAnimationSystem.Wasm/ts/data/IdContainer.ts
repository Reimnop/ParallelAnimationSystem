import { NativeObject, type NativeObjectConstructor } from "../NativeObject";
import { Module } from "../Module";

export class IdContainer<T extends NativeObject> extends NativeObject {
  
  private readonly ctor: NativeObjectConstructor<T>;
  
  constructor(ctor: NativeObjectConstructor<T>, ptr: number, module: Module) {
    super(ptr, module);
    this.ctor = ctor;
  }
  
  get count(): number {
    return this.module.wasm._idContainer_getCount(this.ptr);
  }
  
  insert(item: T): boolean {
    return this.module.wasm._idContainer_insert(this.ptr, item.ptr) !== 0;
  }
  
  remove(id: string): boolean {
    const idPtr = this.module.wasm.stringToNewUTF8(id) as number;
    try {
      return this.module.wasm._idContainer_remove(this.ptr, idPtr) !== 0;
    } finally {
      this.module.wasm._interop_free(idPtr);
    }
  }
  
  getById(id: string): T | null {
    const idPtr = this.module.wasm.stringToNewUTF8(id) as number;
    try {
      const itemPtr = this.module.wasm._idContainer_getById(this.ptr, idPtr);
      if (itemPtr === 0) {
        return null;
      }
      return new this.ctor(itemPtr, this.module);
    } finally {
      this.module.wasm._interop_free(idPtr);
    }
  }
  
  *[Symbol.iterator](): Iterator<[string, T]> {
    const iteratorPtr = this.module.wasm._idContainer_getIterator(this.ptr);
    try {
      while (this.module.wasm._idContainer_iterator_moveNext(iteratorPtr) !== 0) {
        const idPtr = this.module.wasm._idContainer_iterator_getCurrent_key(iteratorPtr);
        const itemPtr = this.module.wasm._idContainer_iterator_getCurrent_value(iteratorPtr);
        try {
          const id = this.module.wasm.UTF8ToString(idPtr);
          const item = new this.ctor(itemPtr, this.module);
          yield [id, item];
        } finally {
          this.module.wasm._interop_free(idPtr);
        }
      }
    } finally {
      this.module.wasm._idContainer_iterator_dispose(iteratorPtr);
      this.module.wasm._interop_free(iteratorPtr);
    }
  }
}