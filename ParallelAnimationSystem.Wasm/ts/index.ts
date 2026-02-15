import PASFactory from "./mod/ParallelAnimationSystem.Wasm";
import { Module } from "./Module";

export default async function createPAS(canvas: HTMLCanvasElement): Promise<Module> {
  const module = await PASFactory();
  return new Module(module, canvas);
}