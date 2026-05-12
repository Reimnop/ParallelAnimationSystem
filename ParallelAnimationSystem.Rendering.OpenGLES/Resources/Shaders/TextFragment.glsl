#version 300 es

precision highp float;

layout(location = 0) out highp vec4 oFragColor;

in highp vec2 vTexCoord;
in highp vec4 vGlyphColor;
flat in highp int vShapeEntryIndex;

// Texture-buffer samplers
uniform highp sampler2D uCurves;
uniform highp isampler2D uCurveIndices;
uniform highp isampler2D uBandEntries;
uniform highp usampler2D uShapeEntries;

struct ShapeEntry {
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

const int TEX_WIDTH = 2048;

QuadraticCurve getCurve(int index) {
    int tIndex = index * 3; // 3 texels per curve
    
    ivec2 coord0 = ivec2(tIndex % TEX_WIDTH, tIndex / TEX_WIDTH);
    vec2 p0 = texelFetch(uCurves, coord0, 0).xy;
    
    ivec2 coord1 = ivec2((tIndex + 1) % TEX_WIDTH, (tIndex + 1) / TEX_WIDTH);
    vec2 p1 = texelFetch(uCurves, coord1, 0).xy;
    
    ivec2 coord2 = ivec2((tIndex + 2) % TEX_WIDTH, (tIndex + 2) / TEX_WIDTH);
    vec2 p2 = texelFetch(uCurves, coord2, 0).xy;
    
    return QuadraticCurve(p0, p1, p2);
}

int getCurveIndex(int index) {
    ivec2 coord = ivec2(index % TEX_WIDTH, index / TEX_WIDTH);
    return texelFetch(uCurveIndices, coord, 0).r;
}

BandEntry getBandEntry(int index) {
    ivec2 coord = ivec2(index % TEX_WIDTH, index / TEX_WIDTH);
    ivec2 data = texelFetch(uBandEntries, coord, 0).xy;
    return BandEntry(data.x, data.y);
}

ShapeEntry getShapeEntry(int index) {
    int tIndex = index * 4; // 4 texels per shape entry
    
    ivec2 coord0 = ivec2(tIndex % TEX_WIDTH, tIndex / TEX_WIDTH);
    uvec4 se0 = texelFetch(uShapeEntries, coord0, 0);
    
    ivec2 coord1 = ivec2((tIndex + 1) % TEX_WIDTH, (tIndex + 1) / TEX_WIDTH);
    uvec4 se1 = texelFetch(uShapeEntries, coord1, 0);
    
    ivec2 coord2 = ivec2((tIndex + 2) % TEX_WIDTH, (tIndex + 2) / TEX_WIDTH);
    uvec4 se2 = texelFetch(uShapeEntries, coord2, 0);
    
    ivec2 coord3 = ivec2((tIndex + 3) % TEX_WIDTH, (tIndex + 3) / TEX_WIDTH);
    uvec4 se3 = texelFetch(uShapeEntries, coord3, 0);
    
    return ShapeEntry(
        int(se0.x), 
        int(se0.y), 
        uintBitsToFloat(se0.z),
        uintBitsToFloat(se0.w),
        int(se1.x),
        int(se1.y), 
        uintBitsToFloat(se1.z), 
        uintBitsToFloat(se1.w),
        uintBitsToFloat(se2.xy), 
        uintBitsToFloat(se2.zw),
        se3.x);
}

// Slug algorithm
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

    float d  = sqrt(max(by * by - ay * p0.y, 0.0));
    float t1 = abs(ay) < 1.0/65536.0 ? p0.y / (2.0 * by) : (by - d) / ay;
    float t2 = abs(ay) < 1.0/65536.0 ? t1               : (by + d) / ay;

    return vec2(
        (ax * t1 - 2.0 * bx) * t1 + p0.x,
        (ax * t2 - 2.0 * bx) * t2 + p0.x);
}

vec2 solveVert(vec2 p0, vec2 p1, vec2 p2) {
    float ax = p0.x - 2.0 * p1.x + p2.x;
    float bx = p0.x - p1.x;
    float ay = p0.y - 2.0 * p1.y + p2.y;
    float by = p0.y - p1.y;

    float d  = sqrt(max(bx * bx - ax * p0.x, 0.0));
    float t1 = abs(ax) < 1.0/65536.0 ? p0.x / (2.0 * bx) : (bx - d) / ax;
    float t2 = abs(ax) < 1.0/65536.0 ? t1               : (bx + d) / ax;

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

vec4 evaluateGlyph(int shapeIndex, vec2 texCoord) {
    ShapeEntry shapeEntry = getShapeEntry(shapeIndex);

    vec2 emsPerPixel = fwidth(texCoord);
    vec2 pixelsPerEm = 1.0 / emsPerPixel;

    int bandX = clamp(int(texCoord.x * shapeEntry.verticalBandScale   + shapeEntry.verticalBandOffset),   0, shapeEntry.verticalBandEntryCount   - 1);
    int bandY = clamp(int(texCoord.y * shapeEntry.horizontalBandScale + shapeEntry.horizontalBandOffset), 0, shapeEntry.horizontalBandEntryCount - 1);

    float xcov = 0.0;
    float xwgt = 0.0;

    BandEntry hband = getBandEntry(shapeEntry.horizontalBandEntryBaseIndex + bandY);
    for (int i = 0; i < hband.curveIndexCount; i++) {
        int curveIdx = getCurveIndex(hband.curveIndexBaseIndex + i);
        QuadraticCurve c = getCurve(curveIdx);

        vec2 p0 = c.p0 - texCoord;
        vec2 p1 = c.p1 - texCoord;
        vec2 p2 = c.p2 - texCoord;

        if (max(max(p0.x, p1.x), p2.x) * pixelsPerEm.x < -0.5) break;

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

    BandEntry vband = getBandEntry(shapeEntry.verticalBandEntryBaseIndex + bandX);
    for (int i = 0; i < vband.curveIndexCount; i++) {
        int curveIdx = getCurveIndex(vband.curveIndexBaseIndex + i);
        QuadraticCurve c = getCurve(curveIdx);

        vec2 p0 = c.p0 - texCoord;
        vec2 p1 = c.p1 - texCoord;
        vec2 p2 = c.p2 - texCoord;

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

    // Per-shape color from the font's own ShapeEntry (RGBA8 packed as uint)
    vec4 shapeColor = vec4(
        float((shapeEntry.color       ) & 0xFFu) / 255.0,
        float((shapeEntry.color >>  8u) & 0xFFu) / 255.0,
        float((shapeEntry.color >> 16u) & 0xFFu) / 255.0,
        float((shapeEntry.color >> 24u) & 0xFFu) / 255.0);

    return vec4(1.0, 1.0, 1.0, coverage) * shapeColor;
}

void main() {
    if (vShapeEntryIndex >= 0) {
        vec4 glyphColor = evaluateGlyph(vShapeEntryIndex, vTexCoord);
        oFragColor = clamp(glyphColor * vGlyphColor, 0.0, 1.0);
    } else {
        oFragColor = clamp(vGlyphColor, 0.0, 1.0);
    }
}
