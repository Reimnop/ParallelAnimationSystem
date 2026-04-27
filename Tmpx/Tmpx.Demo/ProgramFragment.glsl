#version 460 core

layout(location = 0) out vec4 oFragColor;

struct Glyph {
    int horizontalBandEntryBaseIndex;
    int horizontalBandEntryCount;
    float horizontalBandScale;
    float horizontalBandOffset;
    int verticalBandEntryBaseIndex;
    int verticalBandEntryCount;
    float verticalBandScale;
    float verticalBandOffset;
    vec2 min;
    vec2 max;
    uint color;
};

struct QuadraticCurve {
    vec2 p0;
    vec2 p1;
    vec2 p2;
};

struct BandEntry {
    int curveIndexBaseIndex;
    int curveIndexCount;
};

layout(std430, binding = 0) readonly buffer GlyphBuffer { Glyph glyphs[]; };
layout(std430, binding = 1) readonly buffer CurveBuffer { QuadraticCurve curves[]; };
layout(std430, binding = 2) readonly buffer CurveIndexBuffer { int curveIndices[]; };
layout(std430, binding = 3) readonly buffer BandEntryBuffer { BandEntry bandEntries[]; };

in vec2 vTexCoord;
flat in uint vGlyphIndex;

uint calcRootCode(float y1, float y2, float y3) {
    uint i1 = floatBitsToUint(y1) >> 31u;
    uint i2 = floatBitsToUint(y2) >> 30u;
    uint i3 = floatBitsToUint(y3) >> 29u;
    uint shift = (i2 & 2u) | (i1 & ~2u);
    shift = (i3 & 4u) | (shift & ~4u);
    return (0x2E74u >> shift) & 0x0101u;
}

vec2 solveHoriz(vec2 p0, vec2 p1, vec2 p2) {
    float ay = p0.y - 2.0 * p1.y + p2.y;
    float by = p0.y - p1.y;
    float ax = p0.x - 2.0 * p1.x + p2.x;
    float bx = p0.x - p1.x;

    float d = sqrt(max(by * by - ay * p0.y, 0.0));
    float t1 = abs(ay) < 1.0/65536.0 ? p0.y / (2.0 * by) : (by - d) / ay;
    float t2 = abs(ay) < 1.0/65536.0 ? t1                : (by + d) / ay;

    return vec2(
        (ax * t1 - 2.0 * bx) * t1 + p0.x,
        (ax * t2 - 2.0 * bx) * t2 + p0.x);
}

vec2 solveVert(vec2 p0, vec2 p1, vec2 p2) {
    float ax = p0.x - 2.0 * p1.x + p2.x;
    float bx = p0.x - p1.x;
    float ay = p0.y - 2.0 * p1.y + p2.y;
    float by = p0.y - p1.y;

    float d = sqrt(max(bx * bx - ax * p0.x, 0.0));
    float t1 = abs(ax) < 1.0/65536.0 ? p0.x / (2.0 * bx) : (bx - d) / ax;
    float t2 = abs(ax) < 1.0/65536.0 ? t1                : (bx + d) / ax;

    return vec2(
        (ay * t1 - 2.0 * by) * t1 + p0.y,
        (ay * t2 - 2.0 * by) * t2 + p0.y);
}

float calcCoverage(float xcov, float ycov, float xwgt, float ywgt) {
    float coverage = max(
    abs(xcov * xwgt + ycov * ywgt) / max(xwgt + ywgt, 1.0/65536.0),
    min(abs(xcov), abs(ycov)));
    return clamp(coverage, 0.0, 1.0);
}

void main() {
    Glyph glyph = glyphs[vGlyphIndex];

    vec2 emsPerPixel = fwidth(vTexCoord);
    vec2 pixelsPerEm = 1.0 / emsPerPixel;

    int bandX = clamp(int(vTexCoord.x * glyph.verticalBandScale   + glyph.verticalBandOffset),   0, int(glyph.verticalBandEntryCount)   - 1);
    int bandY = clamp(int(vTexCoord.y * glyph.horizontalBandScale + glyph.horizontalBandOffset), 0, int(glyph.horizontalBandEntryCount) - 1);
    
    float xcov = 0.0;
    float xwgt = 0.0;

    // horizontal bands, cast rays in X, indexed by Y band
    BandEntry hband = bandEntries[glyph.horizontalBandEntryBaseIndex + bandY];
    for (int i = 0; i < hband.curveIndexCount; i++) {
        int curveIdx = curveIndices[hband.curveIndexBaseIndex + i];
        QuadraticCurve c = curves[curveIdx];

        vec2 p0 = c.p0 - vTexCoord;
        vec2 p1 = c.p1 - vTexCoord;
        vec2 p2 = c.p2 - vTexCoord;

        // early out, curves are sorted by descending max X
        if (max(max(p0.x, p1.x), p2.x) * pixelsPerEm.x < -0.5) break;

        // nonzero winding contribution from horizontal ray
        float a = p0.y - 2.0 * p1.y + p2.y;
        float b = p0.y - p1.y;
        float c_ = p0.y;

        uint code = calcRootCode(p0.y, p1.y, p2.y);
        if (code != 0u) {
            vec2 r = solveHoriz(p0, p1, p2) * pixelsPerEm.x;
            if ((code & 1u) != 0u) {
                xcov += clamp(r.x + 0.5, 0.0, 1.0);
                xwgt = max(xwgt, clamp(1.0 - abs(r.x) * 2.0, 0.0, 1.0));
            }
            if (code > 1u) {
                xcov -= clamp(r.y + 0.5, 0.0, 1.0);
                xwgt = max(xwgt, clamp(1.0 - abs(r.y) * 2.0, 0.0, 1.0));
            }
        }
    }

    float ycov = 0.0;
    float ywgt = 0.0;

    // vertical bands, cast rays in Y, indexed by X band
    BandEntry vband = bandEntries[glyph.verticalBandEntryBaseIndex + bandX];
    for (int i = 0; i < vband.curveIndexCount; i++) {
        int curveIdx = curveIndices[vband.curveIndexBaseIndex + i];
        QuadraticCurve c = curves[curveIdx];

        vec2 p0 = c.p0 - vTexCoord;
        vec2 p1 = c.p1 - vTexCoord;
        vec2 p2 = c.p2 - vTexCoord;

        if (max(max(p0.y, p1.y), p2.y) * pixelsPerEm.y < -0.5) break;

        uint code = calcRootCode(p0.x, p1.x, p2.x);
        if (code != 0u) {
            vec2 r = solveVert(p0, p1, p2) * pixelsPerEm.y;
            if ((code & 1u) != 0u) {
                ycov -= clamp(r.x + 0.5, 0.0, 1.0);
                ywgt = max(ywgt, clamp(1.0 - abs(r.x) * 2.0, 0.0, 1.0));
            }
            if (code > 1u) {
                ycov += clamp(r.y + 0.5, 0.0, 1.0);
                ywgt = max(ywgt, clamp(1.0 - abs(r.y) * 2.0, 0.0, 1.0));
            }
        }
    }

    float coverage = calcCoverage(xcov, ycov, xwgt, ywgt);

    // unpack color
    vec4 color = vec4(
        float((glyph.color >> 24u) & 0xFFu) / 255.0,
        float((glyph.color >> 16u) & 0xFFu) / 255.0,
        float((glyph.color >>  8u) & 0xFFu) / 255.0,
        float((glyph.color       ) & 0xFFu) / 255.0);
    
    oFragColor = vec4(1.0, 1.0, 1.0, coverage) * color;
}
