export enum BeatmapObjectShape {
  // Square shapes
  SquareSolid       = 0 | 0 << 16,
  SquareHollowThick = 0 | 1 << 16,
  SquareHollowThin  = 0 | 2 << 16,
  SquareCustom      = 0 | 3 << 16,

  // Circle shapes
  CircleSolid             = 1 | 0 << 16,
  CircleHollowThick       = 1 | 1 << 16,
  CircleHalfSolid         = 1 | 2 << 16,
  CircleHalfHollow        = 1 | 3 << 16,
  CircleHollowThin        = 1 | 4 << 16,
  CircleQuarterSolid      = 1 | 5 << 16,
  CircleQuarterHollow     = 1 | 6 << 16,
  CircleHalfQuarterSolid  = 1 | 7 << 16,
  CircleHalfQuarterHollow = 1 | 8 << 16,
  CircleCustom            = 1 | 9 << 16,

  // Triangle shapes
  TriangleSolid             = 2 | 0 << 16,
  TriangleHollow            = 2 | 1 << 16,
  TriangleRightAngledSolid  = 2 | 2 << 16,
  TriangleRightAngledHollow = 2 | 3 << 16,
  TriangleCustom            = 2 | 4 << 16,

  // Arrow shapes
  ArrowNormal = 3 | 0 << 16,
  ArrowHead   = 3 | 1 << 16,

  // Custom shape
  Custom = 3 | 2 << 16,

  // Text shape
  Text = 4 | 0 << 16,

  // Hexagon shape
  HexagonSolid           = 5 | 0 << 16,
  HexagonHollowThick     = 5 | 1 << 16,
  HexagonHollowThin      = 5 | 2 << 16,
  HexagonHalf            = 5 | 3 << 16,
  HexagonHalfHollowThick = 5 | 4 << 16,
  HexagonHalfHollowThin  = 5 | 5 << 16,
  HexagonCustom          = 5 | 6 << 16
}