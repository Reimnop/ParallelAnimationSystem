using System.Runtime.InteropServices;

namespace ParallelAnimationSystem.Rendering;

// https://registry.khronos.org/OpenGL-Refpages/gl4/html/glMultiDrawElementsIndirect.xhtml
[StructLayout(LayoutKind.Sequential, Size = 20)]
public struct DrawElementsIndirectCommand
{
    public required int Count;
    public required int InstanceCount;
    public required int FirstIndex;
    public required int BaseVertex;
    public required int BaseInstance;
}