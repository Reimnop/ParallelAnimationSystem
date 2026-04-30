using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using SimpleStructuredBinaryFormat;
using Tmpx.Common;
using Tmpx.Parsing;
using Tmpx.Shaping;

namespace Tmpx.Demo;

public class DemoWindow() : GameWindow(GameWindowSettings, NativeWindowSettings)
{
    private static GameWindowSettings GameWindowSettings => new();

    private static NativeWindowSettings NativeWindowSettings => new()
    {
        APIVersion = new Version(4, 6)
    };

    private Font font = null!;
    
    private int shapeEntryBuffer, curveBuffer, curveIndexBuffer, bandEntryBuffer, instanceBuffer, vao;
    private int program;

    private int instanceCount;

    protected override void OnLoad()
    {
        base.OnLoad();

        font = ReadFont("Inconsolata-Regular.tmpx");
        
        var gpuShapeEntries = font.ShapeEntries.Select(x => new GpuShapeEntry
        {
            HorizontalBandEntryBaseIndex = x.HorizontalBandEntryBaseIndex,
            HorizontalBandEntryCount = x.HorizontalBandEntryCount,
            HorizontalBandScale = x.HorizontalBandScale,
            HorizontalBandOffset = x.HorizontalBandOffset,
            VerticalBandEntryBaseIndex = x.VerticalBandEntryBaseIndex,
            VerticalBandEntryCount = x.VerticalBandEntryCount,
            VerticalBandScale = x.VerticalBandScale,
            VerticalBandOffset = x.VerticalBandOffset,
            Min = x.Min,
            Max = x.Max,
            Color = x.Color
        }).ToArray();
        
        shapeEntryBuffer = GL.CreateBuffer();
        GL.NamedBufferData(shapeEntryBuffer, gpuShapeEntries.Length * Unsafe.SizeOf<GpuShapeEntry>(), gpuShapeEntries, VertexBufferObjectUsage.StaticDraw);
        
        curveBuffer = GL.CreateBuffer();
        GL.NamedBufferData(curveBuffer, font.Curves.Length * Unsafe.SizeOf<QuadraticCurve>(), font.Curves, VertexBufferObjectUsage.StaticDraw);
        
        curveIndexBuffer = GL.CreateBuffer();
        GL.NamedBufferData(curveIndexBuffer, font.CurveIndices.Length * Unsafe.SizeOf<int>(), font.CurveIndices, VertexBufferObjectUsage.StaticDraw);
        
        bandEntryBuffer = GL.CreateBuffer();
        GL.NamedBufferData(bandEntryBuffer, font.BandEntries.Length * Unsafe.SizeOf<BandEntry>(), font.BandEntries, VertexBufferObjectUsage.StaticDraw);

        var shaper = new Shaper(new DemoFontResolver(font));
        
        var instanceItems = new List<InstanceItem>();
        var text = "<mark=#00000000>        <mark=#000000>  <mark=#00000000>               <mark=#00000000><color=#0000>0</color><br><mark=#00000000>       <mark=#000000> <mark=#f7969f> <mark=#000000> <mark=#00000000>      <mark=#000000>  <mark=#00000000>       <mark=#00000000><color=#0000>0</color><br><mark=#00000000>      <mark=#000000> <mark=#f7969f> <mark=#000000> <mark=#00000000>  <mark=#000000>    <mark=#00000000> <mark=#000000> <mark=#f7969f> <mark=#000000> <mark=#00000000>      <mark=#00000000><color=#0000>0</color><br><mark=#00000000>     <mark=#000000> <mark=#f7969f>  <mark=#000000>   <mark=#f7969f> <mark=#ffffff>   <mark=#000000> <mark=#00000000> <mark=#000000> <mark=#f7969f> <mark=#000000> <mark=#00000000>     <mark=#00000000><color=#0000>0</color><br><mark=#00000000>     <mark=#000000> <mark=#f7969f>  <mark=#000000> <mark=#ffffff>      <mark=#f7969f> <mark=#000000>  <mark=#f7969f>  <mark=#000000> <mark=#00000000>    <mark=#00000000><color=#0000>0</color><br><mark=#00000000>     <mark=#000000> <mark=#f7969f>  <mark=#ffffff>  <mark=#f7969f> <mark=#ffffff>  <mark=#f7969f> <mark=#ffffff> <mark=#f7969f> <mark=#ffffff> <mark=#000000> <mark=#f7969f>  <mark=#000000> <mark=#00000000>    <mark=#00000000><color=#0000>0</color><br><mark=#00000000>     <mark=#000000> <mark=#f7969f> <mark=#ffffff>    <mark=#f7969f> <mark=#ffffff> <mark=#f7969f>   <mark=#ffffff>  <mark=#f7969f>  <mark=#000000> <mark=#00000000>    <mark=#00000000><color=#0000>0</color><br><mark=#00000000>     <mark=#000000> <mark=#ffffff>      <mark=#f7969f>  <mark=#ffffff>    <mark=#f7969f> <mark=#000000> <mark=#00000000>     <mark=#00000000><color=#0000>0</color><br><mark=#00000000>    <mark=#000000> <mark=#f7969f> <mark=#ffffff>             <mark=#000000> <mark=#00000000>     <mark=#00000000><color=#0000>0</color><br><mark=#00000000>    <mark=#000000> <mark=#ffffff>    <mark=#60cb91>  <mark=#ffffff>        <mark=#f7969f> <mark=#000000> <mark=#00000000>    <mark=#00000000><color=#0000>0</color><br><mark=#00000000>   <mark=#000000> <mark=#ffffff>    <mark=#60cb91> <mark=#ffffff>     <mark=#60cb91>  <mark=#ffffff>    <mark=#000000> <mark=#00000000>    <mark=#00000000><color=#0000>0</color><br><mark=#00000000>   <mark=#000000> <mark=#ffffff>  <mark=#60cb91>  <mark=#ffffff> <mark=#000000>  <mark=#ffffff> <mark=#60cb91> <mark=#ffffff>   <mark=#60cb91> <mark=#ffffff>   <mark=#000000> <mark=#00000000>    <mark=#00000000><color=#0000>0</color><br><mark=#00000000>   <mark=#000000> <mark=#ffffff>   <mark=#60cb91> <mark=#000000> <mark=#ffffff>  <mark=#000000> <mark=#60cb91> <mark=#ffffff> <mark=#000000>  <mark=#ffffff> <mark=#60cb91>  <mark=#ffffff>  <mark=#000000> <mark=#00000000>   <mark=#00000000><color=#0000>0</color><br><mark=#00000000>   <mark=#000000> <mark=#ffffff>   <mark=#f7969f> <mark=#60cb91> <mark=#ffffff>  <mark=#60cb91>  <mark=#000000> <mark=#ffffff>  <mark=#000000> <mark=#60cb91> <mark=#ffffff>   <mark=#000000> <mark=#00000000>   <mark=#00000000><color=#0000>0</color><br><mark=#00000000>   <mark=#000000> <mark=#ffffff>   <mark=#000000> <mark=#ffffff> <mark=#60cb91>  <mark=#ffffff>  <mark=#60cb91> <mark=#ffffff>  <mark=#60cb91> <mark=#f7969f> <mark=#ffffff>   <mark=#000000> <mark=#00000000>   <mark=#00000000><color=#0000>0</color><br><mark=#00000000>   <mark=#000000> <mark=#ffffff>   <mark=#000000> <mark=#ffffff>      <mark=#60cb91>  <mark=#ffffff> <mark=#000000> <mark=#ffffff>   <mark=#000000> <mark=#00000000>   <mark=#00000000><color=#0000>0</color><br><mark=#00000000>   <mark=#000000> <mark=#ffffff>    <mark=#000000> <mark=#ffffff> <mark=#000000> <mark=#ffffff>      <mark=#000000> <mark=#ffffff>  <mark=#000000> <mark=#ffffff> <mark=#000000> <mark=#00000000>  <mark=#00000000><color=#0000>0</color><br><mark=#00000000>    <mark=#000000> <mark=#ffffff>   <mark=#f641b4> <mark=#ffffff> <mark=#e50c1d>    <mark=#000000> <mark=#ffffff>  <mark=#000000> <mark=#ffffff>  <mark=#000000> <mark=#ffffff> <mark=#000000> <mark=#00000000>  <mark=#00000000><color=#0000>0</color><br><mark=#00000000>    <mark=#000000> <mark=#f7969f> <mark=#ffffff>  <mark=#f641b4>  <mark=#ffffff> <mark=#e50c1d>   <mark=#ffffff>  <mark=#f641b4> <mark=#000000> <mark=#f7969f> <mark=#000000> <mark=#f7969f> <mark=#ffffff> <mark=#000000>  <mark=#00000000> <mark=#00000000><color=#0000>0</color><br><mark=#00000000>    <mark=#3f705f> <mark=#000000> <mark=#ffffff>   <mark=#f641b4>        <mark=#000000>  <mark=#ffffff> <mark=#f7969f> <mark=#ffffff> <mark=#f7969f>  <mark=#000000> <mark=#00000000><color=#0000>0</color><br><mark=#00000000>   <mark=#3f705f> <mark=#000000>   <mark=#f641b4> <mark=#000000> <mark=#f641b4>       <mark=#000000>   <mark=#ffffff>   <mark=#f7969f> <mark=#ffffff> <mark=#000000> <mark=#00000000><color=#0000>0</color><br><mark=#00000000>  <mark=#3f705f> <mark=#000000>   <mark=#f641b4>   <mark=#000000> <mark=#f641b4>     <mark=#000000>    <mark=#f7969f> <mark=#ffffff>   <mark=#000000> <mark=#00000000> <mark=#00000000><color=#0000>0</color><br><mark=#00000000> <mark=#3f705f> <mark=#000000>    <mark=#f641b4>   <mark=#000000>      <mark=#3f705f> <mark=#000000>      <mark=#3f705f> <mark=#00000000>  <mark=#00000000><color=#0000>0</color><br><mark=#00000000> <mark=#3f705f> <mark=#000000>   <mark=#f641b4>   <mark=#000000>      <mark=#3f705f>  <mark=#000000>     <mark=#3f705f> <mark=#00000000>   <mark=#00000000><color=#0000>0</color><br><mark=#00000000> <mark=#3f705f> <mark=#000000>   <mark=#f641b4>   <mark=#000000> <mark=#3f705f> <mark=#000000>    <mark=#3f705f>  <mark=#000000> <mark=#3f705f> <mark=#000000>  <mark=#3f705f> <mark=#00000000>    <mark=#00000000><color=#0000>0</color><br><mark=#00000000>  <mark=#3f705f> <mark=#000000>     <mark=#60cb91>  <mark=#000000>   <mark=#60cb91>  <mark=#3f705f>  <mark=#000000> <mark=#3f705f>  <mark=#00000000>     <mark=#00000000><color=#0000>0</color><br><mark=#00000000>   <mark=#000000> <mark=#3f705f>  <mark=#60cb91>     <mark=#000000> <mark=#60cb91>   <mark=#3f705f>  <mark=#000000> <mark=#00000000>       <mark=#00000000><color=#0000>0</color><br><mark=#00000000>  <mark=#000000> <mark=#3f705f>   <mark=#60cb91>          <mark=#3f705f>  <mark=#000000> <mark=#00000000>      <mark=#00000000><color=#0000>0</color><br><mark=#00000000> <mark=#000000> <mark=#3f705f>   <mark=#60cb91>            <mark=#3f705f> <mark=#000000> <mark=#00000000>      <mark=#00000000><color=#0000>0</color><br><mark=#00000000>  <mark=#000000> <mark=#3f705f> <mark=#60cb91>              <mark=#3f705f> <mark=#000000> <mark=#00000000>     <mark=#00000000><color=#0000>0</color><br><mark=#00000000>   <mark=#000000>   <mark=#3f705f> <mark=#60cb91>         <mark=#3f705f> <mark=#000000>  <mark=#00000000>      <mark=#00000000><color=#0000>0</color><br><mark=#00000000>     <mark=#3f705f> <mark=#000000>           <mark=#00000000>        <mark=#00000000><color=#0000>0</color><br><mark=#00000000>    <mark=#3f705f> <mark=#f7969f>    <mark=#3f705f> <mark=#00000000>  <mark=#3f705f> <mark=#f7969f>   <mark=#3f705f> <mark=#00000000>        <mark=#00000000><color=#0000>0</color><br><mark=#00000000>    <mark=#3f705f> <mark=#ffffff>   <mark=#3f705f> <mark=#00000000>   <mark=#3f705f> <mark=#ffffff>   <mark=#3f705f> <mark=#00000000>        <mark=#00000000><color=#0000>0</color><br><mark=#00000000>   <mark=#3f705f> <mark=#ffffff>   <mark=#3f705f>  <mark=#00000000>   <mark=#3f705f> <mark=#ffffff>   <mark=#3f705f> <mark=#00000000>        <mark=#00000000><color=#0000>0</color><br><mark=#00000000>   <mark=#3f705f> <mark=#ffffff>    <mark=#3f705f> <mark=#00000000>  <mark=#3f705f>  <mark=#ffffff>   <mark=#3f705f> <mark=#00000000>        <mark=#00000000><color=#0000>0</color><br><mark=#00000000>   <mark=#3f705f> <mark=#ffffff>     <mark=#3f705f>  <mark=#ffffff>     <mark=#3f705f> <mark=#00000000>        <mark=#00000000><color=#0000>0</color><br><mark=#00000000>    <mark=#3f705f> <mark=#ffffff> <mark=#3f705f> <mark=#ffffff> <mark=#3f705f>   <mark=#ffffff> <mark=#3f705f> <mark=#ffffff> <mark=#3f705f> <mark=#ffffff> <mark=#3f705f> <mark=#00000000>        <mark=#00000000><color=#0000>0</color><br><mark=#00000000>     <mark=#3f705f>    <mark=#00000000>  <mark=#3f705f>     <mark=#00000000>         <mark=#00000000><color=#0000>0</color>";
        shaper.Shape(TextParser.Parse(text), TextHorizontalAlignment.Center, TextVerticalAlignment.Middle, "MajorMonoDisplay", (t, c, f, s) =>
        {
            instanceItems.Add(new InstanceItem
            {
                Transform = t,
                Color = c,
                ShapeEntryIndex = f != null ? s : -1,
            });
        });
        
        var instanceData = instanceItems.ToArray();
        
        instanceCount = instanceData.Length;
        
        instanceBuffer = GL.CreateBuffer();
        GL.NamedBufferData(instanceBuffer, instanceData.Length * Unsafe.SizeOf<InstanceItem>(), instanceData, VertexBufferObjectUsage.DynamicDraw);
        
        vao = GL.CreateVertexArray();
        
        GL.VertexArrayVertexBuffer(vao, 0, instanceBuffer, IntPtr.Zero, Unsafe.SizeOf<InstanceItem>());
        
        GL.VertexArrayAttribFormat(vao, 0, 2, VertexAttribType.Float, false, 0);
        GL.VertexArrayAttribBinding(vao, 0, 0);
        GL.EnableVertexArrayAttrib(vao, 0);
        
        GL.VertexArrayAttribFormat(vao, 1, 2, VertexAttribType.Float, false, 2 * sizeof(float));
        GL.VertexArrayAttribBinding(vao, 1, 0);
        GL.EnableVertexArrayAttrib(vao, 1);
        
        GL.VertexArrayAttribFormat(vao, 2, 2, VertexAttribType.Float, false, 4 * sizeof(float));
        GL.VertexArrayAttribBinding(vao, 2, 0);
        GL.EnableVertexArrayAttrib(vao, 2);
        
        GL.VertexArrayAttribIFormat(vao, 3, 1, VertexAttribIType.UnsignedInt, 6 * sizeof(float));
        GL.VertexArrayAttribBinding(vao, 3, 0);
        GL.EnableVertexArrayAttrib(vao, 3);
        
        GL.VertexArrayAttribIFormat(vao, 4, 1, VertexAttribIType.Int, 6 * sizeof(float) + sizeof(uint));
        GL.VertexArrayAttribBinding(vao, 4, 0);
        GL.EnableVertexArrayAttrib(vao, 4);
        
        GL.VertexArrayBindingDivisor(vao, 0, 1);
        
        program = LoadShaderProgram("ProgramVertex.glsl", "ProgramFragment.glsl");
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        var dpi = 96f;
        var pointSize = 8f;
        var pixelsPerEm = pointSize * dpi / 72f;
        
        var modelMatrix = FastMatrix.GetScaleMatrix(pixelsPerEm * 1.234f, pixelsPerEm * 0.598f);
        var cameraMatrix = GetCameraMatrix(Size.Y * 0.5f, Size.X, Size.Y);
        var mvp = modelMatrix * cameraMatrix;
        
        GL.Viewport(0, 0, Size.X, Size.Y);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        
        GL.UseProgram(program);
        GL.BindVertexArray(vao);
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, curveBuffer);
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, curveIndexBuffer);
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 2, bandEntryBuffer);
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 3, shapeEntryBuffer);
        GL.UniformMatrix3x2f(0, 1, false, in mvp);
        GL.Uniform2f(1, Size.X, Size.Y);
        
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
}