using System.Runtime.InteropServices;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Model;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropBeatmapTheme
{
    [UnmanagedCallersOnly(EntryPoint = "beatmapTheme_new")]
    public static IntPtr New(IntPtr idPtr)
    {
        var id = Marshal.PtrToStringUTF8(idPtr);
        if (id is null)
            return IntPtr.Zero;
        var theme = new BeatmapTheme(id);
        return InteropHelper.ObjectToIntPtr(theme);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapTheme_getId")]
    public static IntPtr GetId(IntPtr ptr)
    {
        var theme = InteropHelper.IntPtrToObject<BeatmapTheme>(ptr);
        return InteropHelper.ObjectToIntPtr(theme.Id);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapTheme_getName")]
    public static IntPtr GetName(IntPtr ptr)
    {
        var theme = InteropHelper.IntPtrToObject<BeatmapTheme>(ptr);
        return InteropHelper.ObjectToIntPtr(theme.Name);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapTheme_setName")]
    public static void SetName(IntPtr ptr, IntPtr namePtr)
    {
        var theme = InteropHelper.IntPtrToObject<BeatmapTheme>(ptr);
        var name = Marshal.PtrToStringUTF8(namePtr);
        if (name is null) 
            return;
        theme.Name = name;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "beatmapTheme_getBackgroundColor")]
    public static unsafe void GetBackgroundColor(IntPtr ptr, float* backgroundColorPtr)
    {
        var theme = InteropHelper.IntPtrToObject<BeatmapTheme>(ptr);
        backgroundColorPtr[0] = theme.BackgroundColor.R;
        backgroundColorPtr[1] = theme.BackgroundColor.G;
        backgroundColorPtr[2] = theme.BackgroundColor.B;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "beatmapTheme_setBackgroundColor")]
    public static unsafe void SetBackgroundColor(IntPtr ptr, float* backgroundColorPtr)
    {
        var theme = InteropHelper.IntPtrToObject<BeatmapTheme>(ptr);
        theme.BackgroundColor = new ColorRgb(backgroundColorPtr[0], backgroundColorPtr[1], backgroundColorPtr[2]);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapTheme_getGuiColor")]
    public static unsafe void GetGuiColor(IntPtr ptr, float* guiColorPtr)
    {
        var theme = InteropHelper.IntPtrToObject<BeatmapTheme>(ptr);
        guiColorPtr[0] = theme.GuiColor.R;
        guiColorPtr[1] = theme.GuiColor.G;
        guiColorPtr[2] = theme.GuiColor.B;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "beatmapTheme_setGuiColor")]
    public static unsafe void SetGuiColor(IntPtr ptr, float* guiColorPtr)
    {
        var theme = InteropHelper.IntPtrToObject<BeatmapTheme>(ptr);
        theme.GuiColor = new ColorRgb(guiColorPtr[0], guiColorPtr[1], guiColorPtr[2]);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "beatmapTheme_getGuiAccentColor")]
    public static unsafe void GetGuiAccentColor(IntPtr ptr, float* guiAccentColorPtr)
    {
        var theme = InteropHelper.IntPtrToObject<BeatmapTheme>(ptr);
        guiAccentColorPtr[0] = theme.GuiAccentColor.R;
        guiAccentColorPtr[1] = theme.GuiAccentColor.G;
        guiAccentColorPtr[2] = theme.GuiAccentColor.B;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "beatmapTheme_setGuiAccentColor")]
    public static unsafe void SetGuiAccentColor(IntPtr ptr, float* guiAccentColorPtr)
    {
        var theme = InteropHelper.IntPtrToObject<BeatmapTheme>(ptr);
        theme.GuiAccentColor = new ColorRgb(guiAccentColorPtr[0], guiAccentColorPtr[1], guiAccentColorPtr[2]);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "beatmapTheme_getPlayerColors")]
    public static IntPtr GetPlayerColors(IntPtr ptr)
    {
        var theme = InteropHelper.IntPtrToObject<BeatmapTheme>(ptr);
        return InteropHelper.ObjectToIntPtr(theme.PlayerColors);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "beatmapTheme_getObjectColors")]
    public static IntPtr GetObjectColors(IntPtr ptr)
    {
        var theme = InteropHelper.IntPtrToObject<BeatmapTheme>(ptr);
        return InteropHelper.ObjectToIntPtr(theme.ObjectColors);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "beatmapTheme_getEffectColors")]
    public static IntPtr GetEffectColors(IntPtr ptr)
    {
        var theme = InteropHelper.IntPtrToObject<BeatmapTheme>(ptr);
        return InteropHelper.ObjectToIntPtr(theme.EffectColors);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapTheme_getParallaxObjectColors")]
    public static IntPtr GetParallaxObjectColors(IntPtr ptr)
    {
        var theme = InteropHelper.IntPtrToObject<BeatmapTheme>(ptr);
        return InteropHelper.ObjectToIntPtr(theme.ParallaxObjectColors);
    }
}