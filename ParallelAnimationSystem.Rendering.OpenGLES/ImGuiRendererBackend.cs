using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using OpenTK.Graphics.OpenGLES2;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.DebugUI;
using ParallelAnimationSystem.Mathematics;
using Buffer = OpenTK.Graphics.OpenGL.Buffer;

namespace ParallelAnimationSystem.Rendering.OpenGLES;

public class ImGuiRendererBackend : IImGuiRendererBackend, IOverlayRenderer, IDisposable
{
    public IImGuiRendererBackend.RenderFrameCallback? RenderFrame { get; set; }
    
    private readonly Renderer renderer;
    
    private readonly int vertexArrayHandle, vertexBufferHandle, indexBufferHandle;
    private int vertexBufferSize = 1024, indexBufferSize = 1024;

    private readonly int program;
    private readonly int scaleUniformLocation;
    private readonly int fontTexture, fontSampler;

    private Vector2i currentSize;
    private readonly int framebuffer;
    private int texture;
    
    public ImGuiRendererBackend(ImGuiContext context, ResourceLoader loader, IRenderer renderer)
    {
        this.renderer = (Renderer) renderer;
        this.renderer.AddOverlayRenderer(this);
        
        // Set renderer capabilities
        var io = context.IO;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        
        // Initialize GL resources
        
        // Initialize buffers
        indexBufferHandle = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferHandle);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indexBufferSize, IntPtr.Zero, BufferUsage.DynamicDraw);
        
        vertexBufferHandle = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferHandle);
        GL.BufferData(BufferTarget.ArrayBuffer, vertexBufferSize, IntPtr.Zero, BufferUsage.DynamicDraw);
        
        vertexArrayHandle = GL.GenVertexArray();
        GL.BindVertexArray(vertexArrayHandle);
        
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Unsafe.SizeOf<ImDrawVert>(), 0);
        
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Unsafe.SizeOf<ImDrawVert>(), 8);
        
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, Unsafe.SizeOf<ImDrawVert>(), 16);
        
        // Load and compile shaders
        program = LoaderUtil.LoadShaderProgram(loader, "ImGuiVertex", "ImGuiFragment");
        
        scaleUniformLocation = GL.GetUniformLocation(program, "uScale");
        
        // Load font texture
        io.Fonts.GetTexDataAsRGBA32(out IntPtr fontPixels, out var fontWidth, out var fontHeight, out _);
        
        fontTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, fontTexture);
        GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rgba8, fontWidth, fontHeight);
        GL.TexSubImage2D(TextureTarget.Texture2d, 0, 0, 0, fontWidth, fontHeight, PixelFormat.Rgba, PixelType.UnsignedByte, fontPixels);
        
        fontSampler = GL.GenSampler();
        GL.SamplerParameteri(fontSampler, SamplerParameterI.TextureMinFilter, (int) TextureMinFilter.Linear);
        GL.SamplerParameteri(fontSampler, SamplerParameterI.TextureMagFilter, (int) TextureMagFilter.Linear);
        GL.SamplerParameteri(fontSampler, SamplerParameterI.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
        GL.SamplerParameteri(fontSampler, SamplerParameterI.TextureWrapT, (int) TextureWrapMode.ClampToEdge);
        
        // Set font texture ID
        io.Fonts.SetTexID(fontTexture);
        
        // Create framebuffer
        framebuffer = GL.GenFramebuffer();
        
        // We'll initialize the texture later
    }
    
    public void Dispose()
    {
        renderer.RemoveOverlayRenderer(this);
        
        // GL.DeleteBuffer(vertexBufferHandle);
        // GL.DeleteBuffer(indexBufferHandle);
        // GL.DeleteVertexArray(vertexArrayHandle);
        // GL.DeleteProgram(program);
        // GL.DeleteTexture(fontTexture);
        // GL.DeleteSampler(fontSampler);
        // GL.DeleteFramebuffer(framebuffer);
        // if (texture != 0)
        //     GL.DeleteTexture(texture);
    }
    
    public int ProcessFrame(Vector2i size)
    {
        if (RenderFrame is null)
            return 0;

        var drawDataPtr = RenderFrame();
        
        // Update texture
        if (currentSize != size)
        {
            currentSize = size;
            
            if (texture != 0)
                GL.DeleteTexture(texture);
            
            texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2d, texture);
            GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rgba16f, size.X, size.Y);
            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, texture, 0);
        }
        
        // Bind framebuffer
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
        GL.Viewport(0, 0, size.X, size.Y);
        
        // Clear
        GL.ClearColor(0f, 0f, 0f, 0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        // Disable depth testing
        GL.Disable(EnableCap.DepthTest);
        
        // Enable blending
        GL.Enable(EnableCap.Blend);
        GL.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
        
        // Enable scissor test
        GL.Enable(EnableCap.ScissorTest);
        
        // Bind shader program, vertex array, index buffer and sampler
        GL.BindVertexArray(vertexArrayHandle);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferHandle);
        GL.BindSampler(0, fontSampler);
        
        GL.UseProgram(program);
        GL.Uniform2f(scaleUniformLocation, 1f / size.X, 1f / size.Y);
        
        // Go through all cmd lists
        for (var i = 0; i < drawDataPtr.CmdListsCount; i++)
        {
            var drawList = drawDataPtr.CmdLists[i];
            
            // Upload data to buffers
            var vtxBufferSizeNeeded = drawList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
            if (vtxBufferSizeNeeded > vertexBufferSize)
            {
                vertexBufferSize = vtxBufferSizeNeeded;
                GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferHandle);
                GL.BufferData(BufferTarget.ArrayBuffer, vtxBufferSizeNeeded, drawList.VtxBuffer.Data, BufferUsage.DynamicDraw);
            }
            else
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferHandle);
                GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vtxBufferSizeNeeded, drawList.VtxBuffer.Data);
            }
            
            var idxBufferSizeNeeded = drawList.IdxBuffer.Size * Unsafe.SizeOf<ushort>();
            
            // We don't use GL.BindBuffer here because we already bound it above and it doesn't change
            if (idxBufferSizeNeeded > indexBufferSize)
            {
                indexBufferSize = idxBufferSizeNeeded;
                // GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferHandle);
                GL.BufferData(BufferTarget.ElementArrayBuffer, idxBufferSizeNeeded, drawList.IdxBuffer.Data, BufferUsage.DynamicDraw);
            }
            else
            {
                // GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferHandle);
                GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, idxBufferSizeNeeded, drawList.IdxBuffer.Data);
            }
            
            // Execute draw commands
            for (var j = 0; j < drawList.CmdBuffer.Size; j++)
            {
                var cmd = drawList.CmdBuffer[j];
                
                var clipRect = cmd.ClipRect;
                GL.Scissor(
                    (int) clipRect.X,
                    (int) (size.Y - clipRect.W),
                    (int) (clipRect.Z - clipRect.X),
                    (int) (clipRect.W - clipRect.Y));
                
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2d, (int) cmd.TextureId);
                
                GL.DrawElementsBaseVertex(
                    PrimitiveType.Triangles,
                    (int) cmd.ElemCount,
                    DrawElementsType.UnsignedShort,
                    (IntPtr) (cmd.IdxOffset * Unsafe.SizeOf<ushort>()),
                    (int) cmd.VtxOffset);
            }
        }
        
        // Disable scissor test
        GL.Disable(EnableCap.ScissorTest);
        
        return texture;
    }
}