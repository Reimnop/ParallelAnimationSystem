using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.DebugUI;
using ParallelAnimationSystem.Mathematics;
using Buffer = OpenTK.Graphics.OpenGL.Buffer;

namespace ParallelAnimationSystem.Rendering.OpenGL;

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
        vertexBufferHandle = GL.CreateBuffer();
        GL.NamedBufferData(vertexBufferHandle, vertexBufferSize, IntPtr.Zero, VertexBufferObjectUsage.DynamicDraw);
        
        indexBufferHandle = GL.CreateBuffer();
        GL.NamedBufferData(indexBufferHandle, indexBufferSize, IntPtr.Zero, VertexBufferObjectUsage.DynamicDraw);
        
        vertexArrayHandle = GL.CreateVertexArray();
        
        GL.EnableVertexArrayAttrib(vertexArrayHandle, 0);
        GL.VertexArrayVertexBuffer(vertexArrayHandle, 0, vertexBufferHandle, IntPtr.Zero, Unsafe.SizeOf<ImDrawVert>());
        GL.VertexArrayAttribFormat(vertexArrayHandle, 0, 2, VertexAttribType.Float, false, 0);
        GL.VertexArrayAttribBinding(vertexArrayHandle, 0, 0);
        
        GL.EnableVertexArrayAttrib(vertexArrayHandle, 1);
        GL.VertexArrayVertexBuffer(vertexArrayHandle, 1, vertexBufferHandle, IntPtr.Zero, Unsafe.SizeOf<ImDrawVert>());
        GL.VertexArrayAttribFormat(vertexArrayHandle, 1, 2, VertexAttribType.Float, false, 8);
        GL.VertexArrayAttribBinding(vertexArrayHandle, 1, 1);
        
        GL.EnableVertexArrayAttrib(vertexArrayHandle, 2);
        GL.VertexArrayVertexBuffer(vertexArrayHandle, 2, vertexBufferHandle, IntPtr.Zero, Unsafe.SizeOf<ImDrawVert>());
        GL.VertexArrayAttribFormat(vertexArrayHandle, 2, 4, VertexAttribType.UnsignedByte, true, 16);
        GL.VertexArrayAttribBinding(vertexArrayHandle, 2, 2);
        
        GL.VertexArrayElementBuffer(vertexArrayHandle, indexBufferHandle);
        
        // Load and compile shaders
        program = LoaderUtil.LoadShaderProgram(loader, "ImGuiVertex", "ImGuiFragment");
        
        scaleUniformLocation = GL.GetUniformLocation(program, "uScale");
        
        // Load font texture
        io.Fonts.GetTexDataAsRGBA32(out IntPtr fontPixels, out var fontWidth, out var fontHeight, out _);
        
        fontTexture = GL.CreateTexture(TextureTarget.Texture2d);
        GL.TextureStorage2D(fontTexture, 1, SizedInternalFormat.Rgba8, fontWidth, fontHeight);
        GL.TextureSubImage2D(fontTexture, 0, 0, 0, fontWidth, fontHeight, PixelFormat.Rgba, PixelType.UnsignedByte, fontPixels);
        
        fontSampler = GL.CreateSampler();
        GL.SamplerParameteri(fontSampler, SamplerParameterI.TextureMinFilter, (int) TextureMinFilter.Linear);
        GL.SamplerParameteri(fontSampler, SamplerParameterI.TextureMagFilter, (int) TextureMagFilter.Linear);
        GL.SamplerParameteri(fontSampler, SamplerParameterI.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
        GL.SamplerParameteri(fontSampler, SamplerParameterI.TextureWrapT, (int) TextureWrapMode.ClampToEdge);
        
        // Set font texture ID
        io.Fonts.SetTexID(fontTexture);
        
        // Create framebuffer
        framebuffer = GL.CreateFramebuffer();
        
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
            
            texture = GL.CreateTexture(TextureTarget.Texture2d);
            GL.TextureStorage2D(texture, 1, SizedInternalFormat.Rgba16f, size.X, size.Y);
            GL.NamedFramebufferTexture(framebuffer, FramebufferAttachment.ColorAttachment0, texture, 0);
        }
        
        // Clear texture
        var clearColor = new Vector4(0f, 0f, 0f, 0f);
        GL.ClearNamedFramebufferf(framebuffer, Buffer.Color, 0, in clearColor.X);

        // Bind framebuffer
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
        GL.Viewport(0, 0, size.X, size.Y);
        
        // Disable depth testing
        GL.Disable(EnableCap.DepthTest);
        
        // Enable blending
        GL.Enable(EnableCap.Blend);
        GL.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
        
        // Enable scissor test
        GL.Enable(EnableCap.ScissorTest);
        
        // Bind shader program, vertex array and sampler
        GL.BindVertexArray(vertexArrayHandle);
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
                GL.NamedBufferData(vertexBufferHandle, vtxBufferSizeNeeded, drawList.VtxBuffer.Data, VertexBufferObjectUsage.DynamicDraw);
            }
            else
            {
                GL.NamedBufferSubData(vertexBufferHandle, IntPtr.Zero, vtxBufferSizeNeeded, drawList.VtxBuffer.Data);
            }
            
            var idxBufferSizeNeeded = drawList.IdxBuffer.Size * Unsafe.SizeOf<ushort>();
            if (idxBufferSizeNeeded > indexBufferSize)
            {
                indexBufferSize = idxBufferSizeNeeded;
                GL.NamedBufferData(indexBufferHandle, idxBufferSizeNeeded, drawList.IdxBuffer.Data, VertexBufferObjectUsage.DynamicDraw);
            }
            else
            {
                GL.NamedBufferSubData(indexBufferHandle, IntPtr.Zero, idxBufferSizeNeeded, drawList.IdxBuffer.Data);
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
                
                GL.BindTextureUnit(0, (int) cmd.TextureId);
                
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