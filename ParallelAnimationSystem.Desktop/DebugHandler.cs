using ImGuiNET;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.DebugUI;

namespace ParallelAnimationSystem.Desktop;

public class DebugHandler(DebugAppCore debugAppCore, AudioPlayer audioPlayer) : IDebugHandler
{
    private BloomPostProcessingData? legacyBloom;
    private BloomPostProcessingData? universalBloom;
    
    public void UpdateFrame(ImGuiContext context)
    {
        debugAppCore.OverrideLegacyBloom = legacyBloom;
        debugAppCore.OverrideUniversalBloom = universalBloom;
    }

    public void RenderFrame(ImGuiContext context)
    {
        if (ImGui.Begin("Playback Controls"))
        {
            if (audioPlayer.Playing)
            {
                if (ImGui.Button("Pause"))
                {
                    audioPlayer.Pause();
                }
            }
            else
            {
                if (ImGui.Button("Play"))
                {
                    audioPlayer.Play();
                }
            }
            
            var position = (float) audioPlayer.Position;
            if (ImGui.SliderFloat("Position", ref position, 0.0f, (float) audioPlayer.Length))
            {
                audioPlayer.Position = position;
            }
        }
        ImGui.End();
        
        if (ImGui.Begin("Post Processing"))
        {
            var overrideLegacyBloom = legacyBloom.HasValue;
            if (ImGui.Checkbox("Override Legacy Bloom", ref overrideLegacyBloom))
            {
                if (overrideLegacyBloom)
                {
                    legacyBloom = debugAppCore.OverrideLegacyBloom ?? new BloomPostProcessingData(10.0f, 7.0f);
                }
                else
                {
                    legacyBloom = null;
                }
            }
            
            if (legacyBloom.HasValue)
            {
                var intensity = legacyBloom.Value.Intensity;
                ImGui.SliderFloat("Legacy Bloom Intensity", ref intensity, 0.0f, 100.0f);
                
                var diffusion = legacyBloom.Value.Diffusion;
                ImGui.SliderFloat("Legacy Bloom Diffusion", ref diffusion, 0.0f, 10.0f);
                
                legacyBloom = new BloomPostProcessingData(intensity, diffusion);
            }
            
            var overrideUniversalBloom = universalBloom.HasValue;
            if (ImGui.Checkbox("Override Universal Bloom", ref overrideUniversalBloom))
            {
                if (overrideUniversalBloom)
                {
                    universalBloom = debugAppCore.OverrideUniversalBloom ?? new BloomPostProcessingData(10.0f, 0.8f);
                }
                else
                {
                    universalBloom = null;
                }
            }
            
            if (universalBloom.HasValue)
            {
                var intensity = universalBloom.Value.Intensity;
                ImGui.SliderFloat("Universal Bloom Intensity", ref intensity, 0.0f, 100.0f);
                
                var diffusion = universalBloom.Value.Diffusion;
                ImGui.SliderFloat("Universal Bloom Diffusion", ref diffusion, 0.0f, 1.0f);
                
                universalBloom = new BloomPostProcessingData(intensity, diffusion);
            }
        }
        ImGui.End();
    }
}