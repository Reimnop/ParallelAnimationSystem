import PASFactory from "./ParallelAnimationSystem.Wasm";
import { PASModule } from "./PASModule";

export default async function createPAS(canvas: HTMLCanvasElement): Promise<PASModule> {
  const module = await PASFactory();
  return new PASModule(module, canvas);
}