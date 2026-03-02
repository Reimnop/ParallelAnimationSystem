export enum BeatmapObjectParentType {
  None = 0,
  Position = 0b001,
  Rotation = 0b010,
  Scale = 0b100,
  All = Position | Rotation | Scale
}