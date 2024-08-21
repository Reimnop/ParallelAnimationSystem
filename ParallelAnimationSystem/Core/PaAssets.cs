using OpenTK.Mathematics;

namespace ParallelAnimationSystem.Core;

public static class PaAssets
{
    public static Vector2[] SquareFilledVertices { get; } =
    [
        new Vector2(-0.5f, 0.5f),
        new Vector2(0.5f, 0.5f),
        new Vector2(-0.5f, -0.5f),
        new Vector2(0.5f, -0.5f),
    ];

    public static int[] SquareFilledIndices { get; } =
    [
        0, 1, 2,
        3, 2, 1,
    ];

    public static Vector2[] SquareOutlineVertices { get; } =
    [
        new Vector2(-0.5f, 0.5f),
        new Vector2(0.5f, 0.5f),
        new Vector2(-0.5f, 0.375f),
        new Vector2(0.5f, 0.375f),
        new Vector2(-0.5f, -0.375f),
        new Vector2(0.5f, -0.375f),
        new Vector2(-0.5f, -0.5f),
        new Vector2(0.5f, -0.5f),
        new Vector2(-0.375f, 0.375f),
        new Vector2(-0.375f, -0.375f),
        new Vector2(0.375f, 0.375f),
        new Vector2(0.375f, -0.375f),
    ];

    public static int[] SquareOutlineIndices { get; } =
    [
        0, 1, 2,
        3, 2, 1,
        4, 5, 6,
        7, 6, 5,
        2, 8, 4,
        9, 4, 8,
        10, 3, 11,
        5, 11, 3,
    ];

    public static Vector2[] SquareOutlineThinVertices { get; } =
    [
        new Vector2(-0.5f, 0.5f),
        new Vector2(0.5f, 0.5f),
        new Vector2(-0.5f, 0.45f),
        new Vector2(0.5f, 0.45f),
        new Vector2(-0.5f, -0.45f),
        new Vector2(0.5f, -0.45f),
        new Vector2(-0.5f, -0.5f),
        new Vector2(0.5f, -0.5f),
        new Vector2(-0.45f, 0.45f),
        new Vector2(-0.45f, -0.45f),
        new Vector2(0.45f, 0.45f),
        new Vector2(0.45f, -0.45f),
    ];

    public static int[] SquareOutlineThinIndices { get; } =
    [
        0, 1, 2,
        3, 2, 1,
        4, 5, 6,
        7, 6, 5,
        2, 8, 4,
        9, 4, 8,
        10, 3, 11,
        5, 11, 3,
    ];

    public static Vector2[] CircleFilledVertices { get; } =
    [
        new Vector2(0f, 0f),
        new Vector2(0.5f, 0f),
        new Vector2(0.490393f, 0.0975452f),
        new Vector2(0.46194f, 0.191342f),
        new Vector2(0.415735f, 0.277785f),
        new Vector2(0.353553f, 0.353553f),
        new Vector2(0.277785f, 0.415735f),
        new Vector2(0.191342f, 0.46194f),
        new Vector2(0.0975452f, 0.490393f),
        new Vector2(3.06162e-17f, 0.5f),
        new Vector2(-0.0975452f, 0.490393f),
        new Vector2(-0.191342f, 0.46194f),
        new Vector2(-0.277785f, 0.415735f),
        new Vector2(-0.353553f, 0.353553f),
        new Vector2(-0.415735f, 0.277785f),
        new Vector2(-0.46194f, 0.191342f),
        new Vector2(-0.490393f, 0.0975452f),
        new Vector2(-0.5f, 6.12323e-17f),
        new Vector2(-0.490393f, -0.0975452f),
        new Vector2(-0.46194f, -0.191342f),
        new Vector2(-0.415735f, -0.277785f),
        new Vector2(-0.353553f, -0.353553f),
        new Vector2(-0.277785f, -0.415735f),
        new Vector2(-0.191342f, -0.46194f),
        new Vector2(-0.0975452f, -0.490393f),
        new Vector2(-9.18485e-17f, -0.5f),
        new Vector2(0.0975452f, -0.490393f),
        new Vector2(0.191342f, -0.46194f),
        new Vector2(0.277785f, -0.415735f),
        new Vector2(0.353553f, -0.353553f),
        new Vector2(0.415735f, -0.277785f),
        new Vector2(0.46194f, -0.191342f),
        new Vector2(0.490393f, -0.0975452f),
        new Vector2(0.5f, -1.22465e-16f),
    ];

    public static int[] CircleFilledIndices { get; } =
    [
        0, 1, 2,
        0, 2, 3,
        0, 3, 4,
        0, 4, 5,
        0, 5, 6,
        0, 6, 7,
        0, 7, 8,
        0, 8, 9,
        0, 9, 10,
        0, 10, 11,
        0, 11, 12,
        0, 12, 13,
        0, 13, 14,
        0, 14, 15,
        0, 15, 16,
        0, 16, 17,
        0, 17, 18,
        0, 18, 19,
        0, 19, 20,
        0, 20, 21,
        0, 21, 22,
        0, 22, 23,
        0, 23, 24,
        0, 24, 25,
        0, 25, 26,
        0, 26, 27,
        0, 27, 28,
        0, 28, 29,
        0, 29, 30,
        0, 30, 31,
        0, 31, 32,
        0, 32, 33,
    ];

    public static Vector2[] CircleOutlineVertices { get; } =
    [
        new Vector2(0.375f, 0f),
        new Vector2(0.5f, 0f),
        new Vector2(0.490393f, 0.0975452f),
        new Vector2(0.367794f, 0.0731589f),
        new Vector2(0.46194f, 0.191342f),
        new Vector2(0.346455f, 0.143506f),
        new Vector2(0.415735f, 0.277785f),
        new Vector2(0.311801f, 0.208339f),
        new Vector2(0.353553f, 0.353553f),
        new Vector2(0.265165f, 0.265165f),
        new Vector2(0.277785f, 0.415735f),
        new Vector2(0.208339f, 0.311801f),
        new Vector2(0.191342f, 0.46194f),
        new Vector2(0.143506f, 0.346455f),
        new Vector2(0.0975452f, 0.490393f),
        new Vector2(0.0731589f, 0.367794f),
        new Vector2(3.06162e-17f, 0.5f),
        new Vector2(2.29621e-17f, 0.375f),
        new Vector2(-0.0975452f, 0.490393f),
        new Vector2(-0.0731589f, 0.367794f),
        new Vector2(-0.191342f, 0.46194f),
        new Vector2(-0.143506f, 0.346455f),
        new Vector2(-0.277785f, 0.415735f),
        new Vector2(-0.208339f, 0.311801f),
        new Vector2(-0.353553f, 0.353553f),
        new Vector2(-0.265165f, 0.265165f),
        new Vector2(-0.415735f, 0.277785f),
        new Vector2(-0.311801f, 0.208339f),
        new Vector2(-0.46194f, 0.191342f),
        new Vector2(-0.346455f, 0.143506f),
        new Vector2(-0.490393f, 0.0975452f),
        new Vector2(-0.367794f, 0.0731589f),
        new Vector2(-0.5f, 6.12323e-17f),
        new Vector2(-0.375f, 4.59243e-17f),
        new Vector2(-0.490393f, -0.0975452f),
        new Vector2(-0.367794f, -0.0731589f),
        new Vector2(-0.46194f, -0.191342f),
        new Vector2(-0.346455f, -0.143506f),
        new Vector2(-0.415735f, -0.277785f),
        new Vector2(-0.311801f, -0.208339f),
        new Vector2(-0.353553f, -0.353553f),
        new Vector2(-0.265165f, -0.265165f),
        new Vector2(-0.277785f, -0.415735f),
        new Vector2(-0.208339f, -0.311801f),
        new Vector2(-0.191342f, -0.46194f),
        new Vector2(-0.143506f, -0.346455f),
        new Vector2(-0.0975452f, -0.490393f),
        new Vector2(-0.0731589f, -0.367794f),
        new Vector2(-9.18485e-17f, -0.5f),
        new Vector2(-6.88864e-17f, -0.375f),
        new Vector2(0.0975452f, -0.490393f),
        new Vector2(0.0731589f, -0.367794f),
        new Vector2(0.191342f, -0.46194f),
        new Vector2(0.143506f, -0.346455f),
        new Vector2(0.277785f, -0.415735f),
        new Vector2(0.208339f, -0.311801f),
        new Vector2(0.353553f, -0.353553f),
        new Vector2(0.265165f, -0.265165f),
        new Vector2(0.415735f, -0.277785f),
        new Vector2(0.311801f, -0.208339f),
        new Vector2(0.46194f, -0.191342f),
        new Vector2(0.346455f, -0.143506f),
        new Vector2(0.490393f, -0.0975452f),
        new Vector2(0.367794f, -0.0731589f),
        new Vector2(0.5f, -1.22465e-16f),
        new Vector2(0.375f, -9.18485e-17f),
    ];

    public static int[] CircleOutlineIndices { get; } =
    [
        0, 1, 2,
        0, 3, 2,
        3, 2, 4,
        3, 5, 4,
        5, 4, 6,
        5, 7, 6,
        7, 6, 8,
        7, 9, 8,
        9, 8, 10,
        9, 11, 10,
        11, 10, 12,
        11, 13, 12,
        13, 12, 14,
        13, 15, 14,
        15, 14, 16,
        15, 17, 16,
        17, 16, 18,
        17, 19, 18,
        19, 18, 20,
        19, 21, 20,
        21, 20, 22,
        21, 23, 22,
        23, 22, 24,
        23, 25, 24,
        25, 24, 26,
        25, 27, 26,
        27, 26, 28,
        27, 29, 28,
        29, 28, 30,
        29, 31, 30,
        31, 30, 32,
        31, 33, 32,
        33, 32, 34,
        33, 35, 34,
        35, 34, 36,
        35, 37, 36,
        37, 36, 38,
        37, 39, 38,
        39, 38, 40,
        39, 41, 40,
        41, 40, 42,
        41, 43, 42,
        43, 42, 44,
        43, 45, 44,
        45, 44, 46,
        45, 47, 46,
        47, 46, 48,
        47, 49, 48,
        49, 48, 50,
        49, 51, 50,
        51, 50, 52,
        51, 53, 52,
        53, 52, 54,
        53, 55, 54,
        55, 54, 56,
        55, 57, 56,
        57, 56, 58,
        57, 59, 58,
        59, 58, 60,
        59, 61, 60,
        61, 60, 62,
        61, 63, 62,
        63, 62, 64,
        63, 65, 64,
    ];

    public static Vector2[] CircleHalfVertices { get; } =
    [
        new Vector2(0f, 0f),
        new Vector2(3.06162e-17f, -0.5f),
        new Vector2(0.0975452f, -0.490393f),
        new Vector2(0.191342f, -0.46194f),
        new Vector2(0.277785f, -0.415735f),
        new Vector2(0.353553f, -0.353553f),
        new Vector2(0.415735f, -0.277785f),
        new Vector2(0.46194f, -0.191342f),
        new Vector2(0.490393f, -0.0975452f),
        new Vector2(0.5f, 0f),
        new Vector2(0.490393f, 0.0975452f),
        new Vector2(0.46194f, 0.191342f),
        new Vector2(0.415735f, 0.277785f),
        new Vector2(0.353553f, 0.353553f),
        new Vector2(0.277785f, 0.415735f),
        new Vector2(0.191342f, 0.46194f),
        new Vector2(0.0975452f, 0.490393f),
        new Vector2(3.06162e-17f, 0.5f),
    ];

    public static int[] CircleHalfIndices { get; } =
    [
        0, 1, 2,
        0, 2, 3,
        0, 3, 4,
        0, 4, 5,
        0, 5, 6,
        0, 6, 7,
        0, 7, 8,
        0, 8, 9,
        0, 9, 10,
        0, 10, 11,
        0, 11, 12,
        0, 12, 13,
        0, 13, 14,
        0, 14, 15,
        0, 15, 16,
        0, 16, 17,
    ];

    public static Vector2[] CircleHalfOutlineVertices { get; } =
    [
        new Vector2(2.29621e-17f, -0.375f),
        new Vector2(3.06162e-17f, -0.5f),
        new Vector2(0.0975452f, -0.490393f),
        new Vector2(0.0731589f, -0.367794f),
        new Vector2(0.191342f, -0.46194f),
        new Vector2(0.143506f, -0.346455f),
        new Vector2(0.277785f, -0.415735f),
        new Vector2(0.208339f, -0.311801f),
        new Vector2(0.353553f, -0.353553f),
        new Vector2(0.265165f, -0.265165f),
        new Vector2(0.415735f, -0.277785f),
        new Vector2(0.311801f, -0.208339f),
        new Vector2(0.46194f, -0.191342f),
        new Vector2(0.346455f, -0.143506f),
        new Vector2(0.490393f, -0.0975452f),
        new Vector2(0.367794f, -0.0731589f),
        new Vector2(0.5f, 0f),
        new Vector2(0.375f, 0f),
        new Vector2(0.490393f, 0.0975452f),
        new Vector2(0.367794f, 0.0731589f),
        new Vector2(0.46194f, 0.191342f),
        new Vector2(0.346455f, 0.143506f),
        new Vector2(0.415735f, 0.277785f),
        new Vector2(0.311801f, 0.208339f),
        new Vector2(0.353553f, 0.353553f),
        new Vector2(0.265165f, 0.265165f),
        new Vector2(0.277785f, 0.415735f),
        new Vector2(0.208339f, 0.311801f),
        new Vector2(0.191342f, 0.46194f),
        new Vector2(0.143506f, 0.346455f),
        new Vector2(0.0975452f, 0.490393f),
        new Vector2(0.0731589f, 0.367794f),
        new Vector2(3.06162e-17f, 0.5f),
        new Vector2(2.29621e-17f, 0.375f),
    ];

    public static int[] CircleHalfOutlineIndices { get; } =
    [
        0, 1, 2,
        0, 3, 2,
        3, 2, 4,
        3, 5, 4,
        5, 4, 6,
        5, 7, 6,
        7, 6, 8,
        7, 9, 8,
        9, 8, 10,
        9, 11, 10,
        11, 10, 12,
        11, 13, 12,
        13, 12, 14,
        13, 15, 14,
        15, 14, 16,
        15, 17, 16,
        17, 16, 18,
        17, 19, 18,
        19, 18, 20,
        19, 21, 20,
        21, 20, 22,
        21, 23, 22,
        23, 22, 24,
        23, 25, 24,
        25, 24, 26,
        25, 27, 26,
        27, 26, 28,
        27, 29, 28,
        29, 28, 30,
        29, 31, 30,
        31, 30, 32,
        31, 33, 32,
    ];

    public static Vector2[] CircleOutlineThinVertices { get; } =
    [
        new Vector2(0.45f, 0f),
        new Vector2(0.5f, 0f),
        new Vector2(0.490393f, 0.0975452f),
        new Vector2(0.441353f, 0.0877906f),
        new Vector2(0.46194f, 0.191342f),
        new Vector2(0.415746f, 0.172208f),
        new Vector2(0.415735f, 0.277785f),
        new Vector2(0.374161f, 0.250007f),
        new Vector2(0.353553f, 0.353553f),
        new Vector2(0.318198f, 0.318198f),
        new Vector2(0.277785f, 0.415735f),
        new Vector2(0.250007f, 0.374161f),
        new Vector2(0.191342f, 0.46194f),
        new Vector2(0.172208f, 0.415746f),
        new Vector2(0.0975452f, 0.490393f),
        new Vector2(0.0877906f, 0.441353f),
        new Vector2(3.06162e-17f, 0.5f),
        new Vector2(2.75546e-17f, 0.45f),
        new Vector2(-0.0975452f, 0.490393f),
        new Vector2(-0.0877906f, 0.441353f),
        new Vector2(-0.191342f, 0.46194f),
        new Vector2(-0.172208f, 0.415746f),
        new Vector2(-0.277785f, 0.415735f),
        new Vector2(-0.250007f, 0.374161f),
        new Vector2(-0.353553f, 0.353553f),
        new Vector2(-0.318198f, 0.318198f),
        new Vector2(-0.415735f, 0.277785f),
        new Vector2(-0.374161f, 0.250007f),
        new Vector2(-0.46194f, 0.191342f),
        new Vector2(-0.415746f, 0.172208f),
        new Vector2(-0.490393f, 0.0975452f),
        new Vector2(-0.441353f, 0.0877906f),
        new Vector2(-0.5f, 6.12323e-17f),
        new Vector2(-0.45f, 5.51091e-17f),
        new Vector2(-0.490393f, -0.0975452f),
        new Vector2(-0.441353f, -0.0877906f),
        new Vector2(-0.46194f, -0.191342f),
        new Vector2(-0.415746f, -0.172208f),
        new Vector2(-0.415735f, -0.277785f),
        new Vector2(-0.374161f, -0.250007f),
        new Vector2(-0.353553f, -0.353553f),
        new Vector2(-0.318198f, -0.318198f),
        new Vector2(-0.277785f, -0.415735f),
        new Vector2(-0.250007f, -0.374161f),
        new Vector2(-0.191342f, -0.46194f),
        new Vector2(-0.172208f, -0.415746f),
        new Vector2(-0.0975452f, -0.490393f),
        new Vector2(-0.0877906f, -0.441353f),
        new Vector2(-9.18485e-17f, -0.5f),
        new Vector2(-8.26637e-17f, -0.45f),
        new Vector2(0.0975452f, -0.490393f),
        new Vector2(0.0877906f, -0.441353f),
        new Vector2(0.191342f, -0.46194f),
        new Vector2(0.172208f, -0.415746f),
        new Vector2(0.277785f, -0.415735f),
        new Vector2(0.250007f, -0.374161f),
        new Vector2(0.353553f, -0.353553f),
        new Vector2(0.318198f, -0.318198f),
        new Vector2(0.415735f, -0.277785f),
        new Vector2(0.374161f, -0.250007f),
        new Vector2(0.46194f, -0.191342f),
        new Vector2(0.415746f, -0.172208f),
        new Vector2(0.490393f, -0.0975452f),
        new Vector2(0.441353f, -0.0877906f),
        new Vector2(0.5f, -1.22465e-16f),
        new Vector2(0.45f, -1.10218e-16f),
    ];

    public static int[] CircleOutlineThinIndices { get; } =
    [
        0, 1, 2,
        0, 3, 2,
        3, 2, 4,
        3, 5, 4,
        5, 4, 6,
        5, 7, 6,
        7, 6, 8,
        7, 9, 8,
        9, 8, 10,
        9, 11, 10,
        11, 10, 12,
        11, 13, 12,
        13, 12, 14,
        13, 15, 14,
        15, 14, 16,
        15, 17, 16,
        17, 16, 18,
        17, 19, 18,
        19, 18, 20,
        19, 21, 20,
        21, 20, 22,
        21, 23, 22,
        23, 22, 24,
        23, 25, 24,
        25, 24, 26,
        25, 27, 26,
        27, 26, 28,
        27, 29, 28,
        29, 28, 30,
        29, 31, 30,
        31, 30, 32,
        31, 33, 32,
        33, 32, 34,
        33, 35, 34,
        35, 34, 36,
        35, 37, 36,
        37, 36, 38,
        37, 39, 38,
        39, 38, 40,
        39, 41, 40,
        41, 40, 42,
        41, 43, 42,
        43, 42, 44,
        43, 45, 44,
        45, 44, 46,
        45, 47, 46,
        47, 46, 48,
        47, 49, 48,
        49, 48, 50,
        49, 51, 50,
        51, 50, 52,
        51, 53, 52,
        53, 52, 54,
        53, 55, 54,
        55, 54, 56,
        55, 57, 56,
        57, 56, 58,
        57, 59, 58,
        59, 58, 60,
        59, 61, 60,
        61, 60, 62,
        61, 63, 62,
        63, 62, 64,
        63, 65, 64,
    ];

    public static Vector2[] CircleQuarterVertices { get; } =
    [
        new Vector2(0f, 0f),
        new Vector2(0.5f, 0f),
        new Vector2(0.490393f, 0.0975452f),
        new Vector2(0.46194f, 0.191342f),
        new Vector2(0.415735f, 0.277785f),
        new Vector2(0.353553f, 0.353553f),
        new Vector2(0.277785f, 0.415735f),
        new Vector2(0.191342f, 0.46194f),
        new Vector2(0.0975452f, 0.490393f),
        new Vector2(3.06162e-17f, 0.5f),
    ];

    public static int[] CircleQuarterIndices { get; } =
    [
        0, 1, 2,
        0, 2, 3,
        0, 3, 4,
        0, 4, 5,
        0, 5, 6,
        0, 6, 7,
        0, 7, 8,
        0, 8, 9,
    ];

    public static Vector2[] CircleQuarterOutlineVertices { get; } =
    [
        new Vector2(0.375f, 0f),
        new Vector2(0.5f, 0f),
        new Vector2(0.490393f, 0.0975452f),
        new Vector2(0.367794f, 0.0731589f),
        new Vector2(0.46194f, 0.191342f),
        new Vector2(0.346455f, 0.143506f),
        new Vector2(0.415735f, 0.277785f),
        new Vector2(0.311801f, 0.208339f),
        new Vector2(0.353553f, 0.353553f),
        new Vector2(0.265165f, 0.265165f),
        new Vector2(0.277785f, 0.415735f),
        new Vector2(0.208339f, 0.311801f),
        new Vector2(0.191342f, 0.46194f),
        new Vector2(0.143506f, 0.346455f),
        new Vector2(0.0975452f, 0.490393f),
        new Vector2(0.0731589f, 0.367794f),
        new Vector2(3.06162e-17f, 0.5f),
        new Vector2(2.29621e-17f, 0.375f),
    ];

    public static int[] CircleQuarterOutlineIndices { get; } =
    [
        0, 1, 2,
        0, 3, 2,
        3, 2, 4,
        3, 5, 4,
        5, 4, 6,
        5, 7, 6,
        7, 6, 8,
        7, 9, 8,
        9, 8, 10,
        9, 11, 10,
        11, 10, 12,
        11, 13, 12,
        13, 12, 14,
        13, 15, 14,
        15, 14, 16,
        15, 17, 16,
    ];

    public static Vector2[] CircleHalfQuarterVertices { get; } =
    [
        new Vector2(0f, 0f),
        new Vector2(0.353553f, 0.353553f),
        new Vector2(0.277785f, 0.415735f),
        new Vector2(0.191342f, 0.46194f),
        new Vector2(0.0975452f, 0.490393f),
        new Vector2(3.06162e-17f, 0.5f),
    ];

    public static int[] CircleHalfQuarterIndices { get; } =
    [
        0, 1, 2,
        0, 2, 3,
        0, 3, 4,
        0, 4, 5,
    ];

    public static Vector2[] CircleHalfQuarterOutlineVertices { get; } =
    [
        new Vector2(0.265165f, 0.265165f),
        new Vector2(0.353553f, 0.353553f),
        new Vector2(0.277785f, 0.415735f),
        new Vector2(0.208339f, 0.311801f),
        new Vector2(0.191342f, 0.46194f),
        new Vector2(0.143506f, 0.346455f),
        new Vector2(0.0975452f, 0.490393f),
        new Vector2(0.0731589f, 0.367794f),
        new Vector2(3.06162e-17f, 0.5f),
        new Vector2(2.29621e-17f, 0.375f),
    ];

    public static int[] CircleHalfQuarterOutlineIndices { get; } =
    [
        0, 1, 2,
        0, 3, 2,
        3, 2, 4,
        3, 5, 4,
        5, 4, 6,
        5, 7, 6,
        7, 6, 8,
        7, 9, 8,
    ];

    public static Vector2[] TriangleFilledVertices { get; } =
    [
        new Vector2(3.52086e-17f, 0.575f),
        new Vector2(-0.497965f, -0.2875f),
        new Vector2(0.497965f, -0.2875f),
    ];

    public static int[] TriangleFilledIndices { get; } =
    [
        0, 1, 2,
    ];

    public static Vector2[] TriangleOutlineVertices { get; } =
    [
        new Vector2(3.52086e-17f, 0.575f),
        new Vector2(-0.497965f, -0.2875f),
        new Vector2(2.13889e-17f, 0.349307f),
        new Vector2(-0.302509f, -0.174654f),
        new Vector2(0.302509f, -0.174654f),
        new Vector2(0.497965f, -0.2875f),
    ];

    public static int[] TriangleOutlineIndices { get; } =
    [
        0, 1, 2,
        2, 1, 3,
        3, 1, 4,
        4, 1, 5,
        5, 4, 2,
        2, 0, 5,
    ];

    public static Vector2[] TriangleRightFilledVertices { get; } =
    [
        new Vector2(-0.5f, 0.5f),
        new Vector2(-0.5f, -0.5f),
        new Vector2(0.5f, -0.5f),
    ];

    public static int[] TriangleRightFilledIndices { get; } =
    [
        0, 1, 2,
    ];

    public static Vector2[] TriangleRightOutlineVertices { get; } =
    [
        new Vector2(-0.393333f, 0.236667f),
        new Vector2(-0.5f, 0.5f),
        new Vector2(-0.393333f, -0.393333f),
        new Vector2(-0.5f, -0.5f),
        new Vector2(0.236667f, -0.393333f),
        new Vector2(0.5f, -0.5f),
    ];

    public static int[] TriangleRightOutlineIndices { get; } =
    [
        0, 1, 2,
        2, 1, 3,
        3, 2, 4,
        4, 3, 5,
        5, 4, 0,
        0, 5, 1,
    ];

    public static Vector2[] ArrowVertices { get; } =
    [
        new Vector2(0.114711f, 0.0941153f),
        new Vector2(-0.385289f, 0.0941153f),
        new Vector2(-0.385289f, -0.0941153f),
        new Vector2(0.114711f, -0.0941153f),
        new Vector2(0.0131641f, 0.403351f),
        new Vector2(0.495352f, 0f),
        new Vector2(-0.0995f, 0.272262f),
        new Vector2(-0.0995f, -0.272262f),
        new Vector2(0.0131641f, -0.403351f),
    ];

    public static int[] ArrowIndices { get; } =
    [
        0, 1, 2,
        2, 3, 0,
        4, 5, 6,
        6, 5, 0,
        0, 5, 3,
        3, 5, 7,
        7, 5, 8,
    ];

    public static Vector2[] ArrowHeadVertices { get; } =
    [
        new Vector2(0.0131641f, 0.403352f),
        new Vector2(0.495352f, 2.4e-07f),
        new Vector2(-0.0995f, 0.272262f),
        new Vector2(0.218477f, 2.4e-07f),
        new Vector2(-0.0995f, -0.272262f),
        new Vector2(0.0131641f, -0.403351f),
    ];

    public static int[] ArrowHeadIndices { get; } =
    [
        0, 1, 2,
        2, 1, 3,
        3, 1, 4,
        4, 1, 5,
    ];

    public static Vector2[] HexagonFilledVertices { get; } =
    [
        new Vector2(0f, 0f),
        new Vector2(3.06162e-17f, 0.5f),
        new Vector2(-0.433013f, 0.25f),
        new Vector2(-0.433013f, -0.25f),
        new Vector2(-9.18485e-17f, -0.5f),
        new Vector2(0.433013f, -0.25f),
        new Vector2(0.433013f, 0.25f),
        new Vector2(1.53081e-16f, 0.5f),
    ];

    public static int[] HexagonFilledIndices { get; } =
    [
        0, 1, 2,
        0, 2, 3,
        0, 3, 4,
        0, 4, 5,
        0, 5, 6,
        0, 6, 7,
    ];

    public static Vector2[] HexagonOutlineVertices { get; } =
    [
        new Vector2(2.13889e-17f, 0.349307f),
        new Vector2(3.06162e-17f, 0.5f),
        new Vector2(-0.433013f, 0.25f),
        new Vector2(-0.302509f, 0.174654f),
        new Vector2(-0.433013f, -0.25f),
        new Vector2(-0.302509f, -0.174654f),
        new Vector2(-9.18485e-17f, -0.5f),
        new Vector2(-6.41667e-17f, -0.349307f),
        new Vector2(0.433013f, -0.25f),
        new Vector2(0.302509f, -0.174654f),
        new Vector2(0.433013f, 0.25f),
        new Vector2(0.302509f, 0.174654f),
        new Vector2(1.53081e-16f, 0.5f),
        new Vector2(1.06944e-16f, 0.349307f),
    ];

    public static int[] HexagonOutlineIndices { get; } =
    [
        0, 1, 2,
        0, 3, 2,
        3, 2, 4,
        3, 5, 4,
        5, 4, 6,
        5, 7, 6,
        7, 6, 8,
        7, 9, 8,
        9, 8, 10,
        9, 11, 10,
        11, 10, 12,
        11, 13, 12,
    ];

    public static Vector2[] HexagonOutlineThinVertices { get; } =
    [
        new Vector2(2.78607e-17f, 0.455f),
        new Vector2(3.06162e-17f, 0.5f),
        new Vector2(-0.433013f, 0.25f),
        new Vector2(-0.394042f, 0.2275f),
        new Vector2(-0.433013f, -0.25f),
        new Vector2(-0.394042f, -0.2275f),
        new Vector2(-9.18485e-17f, -0.5f),
        new Vector2(-8.35821e-17f, -0.455f),
        new Vector2(0.433013f, -0.25f),
        new Vector2(0.394042f, -0.2275f),
        new Vector2(0.433013f, 0.25f),
        new Vector2(0.394042f, 0.2275f),
        new Vector2(1.53081e-16f, 0.5f),
        new Vector2(1.39304e-16f, 0.455f),
    ];

    public static int[] HexagonOutlineThinIndices { get; } =
    [
        0, 1, 2,
        0, 3, 2,
        3, 2, 4,
        3, 5, 4,
        5, 4, 6,
        5, 7, 6,
        7, 6, 8,
        7, 9, 8,
        9, 8, 10,
        9, 11, 10,
        11, 10, 12,
        11, 13, 12,
    ];

    public static Vector2[] HexagonHalfVertices { get; } =
    [
        new Vector2(0f, 0.5f),
        new Vector2(0f, -0.5f),
        new Vector2(0.433013f, 0.25f),
        new Vector2(0.433013f, -0.25f),
    ];

    public static int[] HexagonHalfIndices { get; } =
    [
        0, 1, 2,
        2, 1, 3,
    ];

    public static Vector2[] HexagonHalfOutlineVertices { get; } =
    [
        new Vector2(0f, 0.5f),
        new Vector2(0f, 0.35f),
        new Vector2(0.433013f, 0.25f),
        new Vector2(0.303109f, 0.175f),
        new Vector2(0.433013f, -0.25f),
        new Vector2(0.303109f, -0.175f),
        new Vector2(0f, -0.5f),
        new Vector2(0f, -0.35f),
    ];

    public static int[] HexagonHalfOutlineIndices { get; } =
    [
        0, 1, 2,
        2, 1, 3,
        3, 2, 4,
        4, 3, 5,
        5, 4, 6,
        6, 5, 7,
    ];

    public static Vector2[] HexagonHalfOutlineThinVertices { get; } =
    [
        new Vector2(0f, 0.5f),
        new Vector2(0f, 0.45f),
        new Vector2(0.433013f, 0.25f),
        new Vector2(0.394042f, 0.2275f),
        new Vector2(0.433013f, -0.25f),
        new Vector2(0.394042f, -0.2275f),
        new Vector2(0f, -0.5f),
        new Vector2(0f, -0.45f),
    ];

    public static int[] HexagonHalfOutlineThinIndices { get; } =
    [
        0, 1, 2,
        2, 1, 3,
        3, 2, 4,
        4, 3, 5,
        5, 4, 6,
        6, 5, 7,
    ];
}