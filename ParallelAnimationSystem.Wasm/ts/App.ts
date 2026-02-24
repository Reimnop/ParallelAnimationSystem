import { NativeObject } from "./NativeObject";
import { RandomSeedService } from "./service/RandomSeedService";
import { BeatmapService } from "./service/BeatmapService";

export class App extends NativeObject {
  private cachedRandomSeedService: RandomSeedService | null = null;
  private cachedBeatmapService: BeatmapService | null = null;
  
  get randomSeedService(): RandomSeedService {
    if (this.cachedRandomSeedService) {
      return this.cachedRandomSeedService;
    }
    
    const ptr = this.wasm._app_getRandomSeedService(this.ptr);
    return this.cachedRandomSeedService = new RandomSeedService(this.module, ptr);
  }
  
  get beatmapService(): BeatmapService {
    if (this.cachedBeatmapService) {
      return this.cachedBeatmapService;
    }
    
    const ptr = this.wasm._app_getBeatmapService(this.ptr);
    return this.cachedBeatmapService = new BeatmapService(this.module, ptr);
  }
  
  processFrame(time: number): void {
    this.wasm._app_processFrame(this.ptr, time);
  }
}