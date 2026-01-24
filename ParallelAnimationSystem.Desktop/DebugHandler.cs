using ImGuiNET;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.DebugUI;

namespace ParallelAnimationSystem.Desktop;

public class DebugHandler(DebugAppCore debugAppCore, AudioPlayer audioPlayer) : IDebugHandler
{
    private BloomPostProcessingData? bloom;
    
    public void UpdateFrame(ImGuiContext context)
    {
        debugAppCore.OverrideBloom = bloom;
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
            
            ImGui.End();
        }
        
        if (ImGui.Begin("Post Processing"))
        {
            var overrideBloom = bloom.HasValue;
            if (ImGui.Checkbox("Override Bloom", ref overrideBloom))
            {
                if (overrideBloom)
                {
                    bloom = debugAppCore.OverrideBloom ?? new BloomPostProcessingData(10.0f, 5.0f);
                }
                else
                {
                    bloom = null;
                }
            }
            
            if (bloom.HasValue)
            {
                var intensity = bloom.Value.Intensity;
                ImGui.SliderFloat("Bloom Intensity", ref intensity, 0.0f, 50.0f);
                
                var diffusion = bloom.Value.Diffusion;
                ImGui.SliderFloat("Bloom Diffusion", ref diffusion, 1.0f, 10.0f);
                
                bloom = new BloomPostProcessingData(intensity, diffusion);
            }
            
            ImGui.End();
        }
    }
}