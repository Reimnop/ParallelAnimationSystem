#version 460 core

layout(location = 0) out vec4 oFragColor;

in vec4 vColor1;
in vec4 vColor2;
in vec2 vTexCoord;
in flat int vRenderMode;
in flat int vRenderType;
in flat int vShapeIndex;

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

layout(std430, binding = 2) readonly buffer CurveBuffer { QuadraticCurve curves[]; };
layout(std430, binding = 3) readonly buffer CurveIndexBuffer { int curveIndices[]; };
layout(std430, binding = 4) readonly buffer BandEntryBuffer { BandEntry bandEntries[]; };
layout(std430, binding = 5) readonly buffer ShapeEntryBuffer { ShapeEntry shapeEntries[]; };

vec4 getColor(vec4 color1, vec4 color2, int mode, vec2 uv) {
    // mode 0: color1
    // mode 1: gradient 2 to 1 on x
    // mode 2: gradient 1 to 2 on x
    // mode 3: circular gradient outwards
    // mode 4: circular gradient inwards
    if (mode == 0) {
        return color1;
    } else if (mode == 1) {
        return mix(color2, color1, uv.x);
    } else if (mode == 2) {
        return mix(color1, color2, uv.x);
    } else if (mode == 3) {
        float dist = min(distance(uv, vec2(0.5)) * 2.0, 1.0);
        return mix(color2, color1, dist);
    } else if (mode == 4) {
        float dist = min(distance(uv, vec2(0.5)) * 2.0, 1.0);
        return mix(color1, color2, dist);
    }
    return color1;
}

// TMPX curve coverage

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

vec4 evaluateGlyph(int shapeIndex, vec2 texCoord) {
    ShapeEntry shapeEntry = shapeEntries[shapeIndex];

    vec2 emsPerPixel = fwidth(texCoord);
    vec2 pixelsPerEm = 1.0 / emsPerPixel;

    int bandX = clamp(int(texCoord.x * shapeEntry.verticalBandScale   + shapeEntry.verticalBandOffset),   0, shapeEntry.verticalBandEntryCount   - 1);
    int bandY = clamp(int(texCoord.y * shapeEntry.horizontalBandScale + shapeEntry.horizontalBandOffset), 0, shapeEntry.horizontalBandEntryCount - 1);

    float xcov = 0.0;
    float xwgt = 0.0;

    BandEntry hband = bandEntries[shapeEntry.horizontalBandEntryBaseIndex + bandY];
    for (int i = 0; i < hband.curveIndexCount; i++) {
        int curveIdx = curveIndices[hband.curveIndexBaseIndex + i];
        QuadraticCurve c = curves[curveIdx];

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

    BandEntry vband = bandEntries[shapeEntry.verticalBandEntryBaseIndex + bandX];
    for (int i = 0; i < vband.curveIndexCount; i++) {
        int curveIdx = curveIndices[vband.curveIndexBaseIndex + i];
        QuadraticCurve c = curves[curveIdx];

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
    if (vRenderType == 1) {
        // Text path: glyph (vShapeIndex >= 0) or mark rectangle (-1)
        if (vShapeIndex >= 0) {
            vec4 glyphColor = evaluateGlyph(vShapeIndex, vTexCoord);
            oFragColor = glyphColor * vColor1;
        } else {
            oFragColor = vColor1;
        }
    } else {
        oFragColor = getColor(vColor1, vColor2, vRenderMode, vTexCoord);
    }

    oFragColor = clamp(oFragColor, 0.0, 1.0);
}