# ParallelAnimationSystem.Wasm

> Web-friendly module for Parallel Animation System, a Project Arrhythmia beatmap viewer

## Usage

```sh
npm install paswasm
```

If you're using Vite, add this to your `vite.config.js`:

```js
optimizeDeps: {
  exclude: ['paswasm']
}
```

## Example

```js
import createPAS from "paswasm";
import { BeatmapFormat } from "paswasm/data/BeatmapFormat";

const pas = await createPAS(canvas);
pas.start(true, true); // enable post processing and text rendering

const app = pas.app;
if (!app) {
  console.error("failed to initialize PAS");
  return;
}

// fetch beatmap data
const beatmapDataResponse = await fetch("/path/to/level.vgd");
const beatmapData = await beatmapDataResponse.text();

// load beatmap into PAS
const beatmapService = app.beatmapService;
beatmapService.loadBeatmap(beatmapData, BeatmapFormat.Vgd);

// enter main render loop
const update = (time) => {
  app.processFrame(time / 1000); // convert time from millsecond to second
  requestAnimationFrame(update);
};

requestAnimationFrame(update);
```
