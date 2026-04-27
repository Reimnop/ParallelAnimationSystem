using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using SimpleStructuredBinaryFormat;
using Tmpx.Common;

namespace Tmpx.Demo;

public class DemoWindow() : GameWindow(GameWindowSettings, NativeWindowSettings)
{
    private static GameWindowSettings GameWindowSettings => new();

    private static NativeWindowSettings NativeWindowSettings => new()
    {
        APIVersion = new Version(4, 6)
    };

    private Font font = null!;
    
    private int glyphBuffer, curveBuffer, curveIndexBuffer, bandEntryBuffer, instanceBuffer, vao;
    private int program;

    private int instanceCount;

    protected override void OnLoad()
    {
        base.OnLoad();

        font = ReadFont("LiberationSans-Regular.tmpx");
        
        var gpuGlyphs = font.Glyphs.Select(x => new GpuGlyph
        {
            HorizontalBandEntryBaseIndex = x.Bands.HorizontalBandEntryBaseIndex,
            HorizontalBandEntryCount = x.Bands.HorizontalBandEntryCount,
            HorizontalBandScale = x.Bands.HorizontalBandScale,
            HorizontalBandOffset = x.Bands.HorizontalBandOffset,
            VerticalBandEntryBaseIndex = x.Bands.VerticalBandEntryBaseIndex,
            VerticalBandEntryCount = x.Bands.VerticalBandEntryCount,
            VerticalBandScale = x.Bands.VerticalBandScale,
            VerticalBandOffset = x.Bands.VerticalBandOffset,
            Min = x.Metrics.Min,
            Max = x.Metrics.Max,
            Color = x.Color
        }).ToArray();
        
        glyphBuffer = GL.CreateBuffer();
        GL.NamedBufferData(glyphBuffer, gpuGlyphs.Length * Unsafe.SizeOf<GpuGlyph>(), gpuGlyphs, VertexBufferObjectUsage.StaticDraw);
        
        curveBuffer = GL.CreateBuffer();
        GL.NamedBufferData(curveBuffer, font.Curves.Length * Unsafe.SizeOf<QuadraticCurve>(), font.Curves, VertexBufferObjectUsage.StaticDraw);
        
        curveIndexBuffer = GL.CreateBuffer();
        GL.NamedBufferData(curveIndexBuffer, font.CurveIndices.Length * Unsafe.SizeOf<int>(), font.CurveIndices, VertexBufferObjectUsage.StaticDraw);
        
        bandEntryBuffer = GL.CreateBuffer();
        GL.NamedBufferData(bandEntryBuffer, font.BandEntries.Length * Unsafe.SizeOf<BandEntry>(), font.BandEntries, VertexBufferObjectUsage.StaticDraw);

        var instanceData = ShapeText("Hello, World!", font);
        instanceCount = instanceData.Length;
        
        instanceBuffer = GL.CreateBuffer();
        GL.NamedBufferData(instanceBuffer, instanceData.Length * Unsafe.SizeOf<InstanceItem>(), instanceData, VertexBufferObjectUsage.DynamicDraw);
        
        vao = GL.CreateVertexArray();
        
        GL.VertexArrayVertexBuffer(vao, 0, instanceBuffer, IntPtr.Zero, Unsafe.SizeOf<InstanceItem>());
        
        GL.VertexArrayAttribFormat(vao, 0, 2, VertexAttribType.Float, false, 0);
        GL.VertexArrayAttribBinding(vao, 0, 0);
        GL.EnableVertexArrayAttrib(vao, 0);
        
        GL.VertexArrayAttribIFormat(vao, 1, 1, VertexAttribIType.Int, (uint)Unsafe.SizeOf<Vector2>());
        GL.VertexArrayAttribBinding(vao, 1, 0);
        GL.EnableVertexArrayAttrib(vao, 1);
        
        GL.VertexArrayBindingDivisor(vao, 0, 1);
        
        program = LoadShaderProgram("ProgramVertex.glsl", "ProgramFragment.glsl");
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        var dpi = 96f;
        var pointSize = 64f;
        var pixelsPerEm = pointSize * dpi / 72f;
        
        var modelMatrix = FastMatrix.GetScaleMatrix(pixelsPerEm, pixelsPerEm);
        var cameraMatrix = GetCameraMatrix(Size.Y * 0.5f, Size.X, Size.Y);
        var mvp = modelMatrix * cameraMatrix;
        
        GL.Viewport(0, 0, Size.X, Size.Y);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        
        GL.UseProgram(program);
        GL.BindVertexArray(vao);
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, glyphBuffer);
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, curveBuffer);
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 2, curveIndexBuffer);
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 3, bandEntryBuffer);
        GL.UniformMatrix3x2f(0, 1, false, in mvp);
        
        GL.DrawArraysInstanced(PrimitiveType.TriangleStrip, 0, 4, instanceCount);
        
        SwapBuffers();
    }

    private static Font ReadFont(string path)
    {
        using var stream = File.OpenRead(path);
        var ssbf = (SsbfObject) SsbfRead.ReadFromStream(stream)!;
        return TmpxReader.Read(ssbf);
    }
    
    private static Matrix3x2 GetCameraMatrix(float scale, int width, int height)
    {
        var aspectRatio = width / (float)height;
        var inversionResult = Matrix3x2.Invert(Matrix3x2.CreateScale(Vector2.One * scale), out var view);
        Debug.Assert(inversionResult);
        
        var projection = Matrix3x2.CreateScale(1.0f / aspectRatio, 1.0f);
        return view * projection;
    }

    private static int LoadShaderProgram(string vertexShaderPath, string fragmentShaderPath)
    {
        var vertexShaderSource = File.ReadAllText(vertexShaderPath);
        var fragmentShaderSource = File.ReadAllText(fragmentShaderPath);
        
        var vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);
        
        var compileStatus = GL.GetShaderi(vertexShader, ShaderParameterName.CompileStatus);
        if (compileStatus == 0)
        {
            GL.GetShaderInfoLog(vertexShader, out var infoLog);
            throw new Exception($"Failed to compile vertex shader: {infoLog}");
        }
        
        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        GL.CompileShader(fragmentShader);
        
        compileStatus = GL.GetShaderi(fragmentShader, ShaderParameterName.CompileStatus);
        if (compileStatus == 0)
        {
            GL.GetShaderInfoLog(fragmentShader, out var infoLog);
            throw new Exception($"Failed to compile fragment shader: {infoLog}");
        }
        
        var program = GL.CreateProgram();
        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);
        GL.LinkProgram(program);
        
        var linkStatus = GL.GetProgrami(program, ProgramProperty.LinkStatus);
        if (linkStatus == 0)
        {
            GL.GetProgramInfoLog(program, out var infoLog);
            throw new Exception($"Failed to link program: {infoLog}");
        }
        
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
        
        return program;
    }
    
    public static InstanceItem[] ShapeText(string text, Font font)
    {
        var instances = new List<InstanceItem>();
        var x = 0f;
    
        foreach (var c in text)
        {
            if (!font.GlyphMap.TryGetValue(c, out var glyphIndex))
                glyphIndex = font.GlyphMap['?'];
        
            var glyph = font.Glyphs[glyphIndex];
        
            if (glyph.Bands.HorizontalBandEntryCount > 0 || glyph.Bands.VerticalBandEntryCount > 0)
                instances.Add(new InstanceItem
                {
                    Position = new Vector2(x, 0f),
                    GlyphIndex = glyphIndex
                });
        
            x += glyph.Metrics.AdvanceWidth;
        }
    
        var totalWidth = x;
        for (var i = 0; i < instances.Count; i++)
            instances[i] = instances[i] with { Position = instances[i].Position - new Vector2(totalWidth * 0.5f, 0f) };
    
        return instances.ToArray();
    }
}