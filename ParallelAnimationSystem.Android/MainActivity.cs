using System.Diagnostics;
using Android.Content;
using Android.Content.PM;
using Android.Provider;
using Google.Android.Material.Button;
using Google.Android.Material.MaterialSwitch;
using ParallelAnimationSystem.Core;
using Activity = Android.App.Activity;
using Uri = Android.Net.Uri;

namespace ParallelAnimationSystem.Android;

[Activity(
    Label = "@string/app_name",
    Theme = "@style/app_theme",
    ScreenOrientation = ScreenOrientation.Portrait, 
    MainLauncher = true)]
public class MainActivity : Activity
{
    private const int RequestCodeBeatmap = 0;
    private const int RequestCodeAudio = 1;
    
    private MaterialSwitch lockAspectRatioSwitch = null!;
    private MaterialSwitch enablePostProcessingSwitch = null!;
    private MaterialSwitch enableTextRenderingSwitch = null!;
    private MaterialButton chooseBeatmapFileButton = null!;
    private MaterialButton chooseAudioFileButton = null!;

    private Uri? beatmapPath;
    private BeatmapFormat beatmapFormat;
    private Uri? audioPath;
    
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_main);

        lockAspectRatioSwitch = FindViewById<MaterialSwitch>(Resource.Id.lock_aspect_ratio)!;
        enablePostProcessingSwitch = FindViewById<MaterialSwitch>(Resource.Id.enable_post_processing)!;
        enableTextRenderingSwitch = FindViewById<MaterialSwitch>(Resource.Id.enable_text_rendering)!;
        chooseBeatmapFileButton = FindViewById<MaterialButton>(Resource.Id.choose_beatmap_file)!;
        chooseAudioFileButton = FindViewById<MaterialButton>(Resource.Id.choose_audio_file)!;
        
        chooseBeatmapFileButton.Click += OnChooseBeatmapFileButtonClick;
        chooseAudioFileButton.Click += OnChooseAudioFileButtonClick;

        var playButton = FindViewById<MaterialButton>(Resource.Id.play_button)!;
        playButton.Click += OnStartButtonClick;
    }
    
    private void OnChooseBeatmapFileButtonClick(object? sender, EventArgs e)
    {
        var intent = new Intent(Intent.ActionOpenDocument);
        intent.AddCategory(Intent.CategoryOpenable);
        intent.SetType("*/*");
        StartActivityForResult(intent, RequestCodeBeatmap);
    }

    private void OnChooseAudioFileButtonClick(object? sender, EventArgs e)
    {
        var intent = new Intent(Intent.ActionOpenDocument);
        intent.AddCategory(Intent.CategoryOpenable);
        intent.SetType("audio/ogg");
        StartActivityForResult(intent, RequestCodeAudio);
    }
    
    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        
        if (resultCode != Result.Ok || data is null)
            return;

        var uri = data.Data;
        if (uri is null)
            return;

        var fileName = GetFileName(uri);

        switch (requestCode)
        {
            case RequestCodeBeatmap:
            {
                // Check file extension
                var extension = Path.GetExtension(fileName);
                if (extension.Equals(".lsb", StringComparison.InvariantCultureIgnoreCase) ||
                    extension.Equals(".vgd", StringComparison.InvariantCultureIgnoreCase))
                {
                    beatmapPath = uri;
                    beatmapFormat = extension.ToLowerInvariant() switch
                    {
                        ".lsb" => BeatmapFormat.Lsb,
                        ".vgd" => BeatmapFormat.Vgd,
                        _ => throw new NotSupportedException($"Unsupported beatmap format '{extension}'")
                    };
                    
                    chooseBeatmapFileButton.Text = fileName;
                }
                break;
            }
            case RequestCodeAudio:
            {
                audioPath = uri;
                
                chooseAudioFileButton.Text = fileName;
                break;
            }
        }
    }

    private string GetFileName(Uri uri)
    {
        var contentResolver = ContentResolver;
        Debug.Assert(contentResolver is not null);
        
        string? fileName = null;
        var cursor = contentResolver.Query(uri, null, null, null, null);
        if (cursor is null)
            return string.Empty;
        var nameIndex = cursor.GetColumnIndex(IOpenableColumns.DisplayName);
        if (cursor.MoveToFirst())
            fileName = cursor.GetString(nameIndex);
        return fileName ?? string.Empty;
    }

    private void OnStartButtonClick(object? sender, EventArgs e)
    {
        if (beatmapPath is null)
            return;
        
        if (audioPath is null)
            return;
        
        var intent = new Intent(this, typeof(PasActivity));
        intent.PutExtra("lockAspectRatio", lockAspectRatioSwitch.Checked);
        intent.PutExtra("postProcessing", enablePostProcessingSwitch.Checked);
        intent.PutExtra("textRendering", enableTextRenderingSwitch.Checked);
        intent.PutExtra("beatmapPath", beatmapPath);
        intent.PutExtra("beatmapFormat", (int) beatmapFormat);
        intent.PutExtra("audioPath", audioPath);
        StartActivity(intent);
    }
}