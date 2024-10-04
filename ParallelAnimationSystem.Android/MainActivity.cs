using Android.Content;
using Android.Content.PM;
using AndroidX.AppCompat.App;

namespace ParallelAnimationSystem.Android;

[Activity(
    Label = "@string/app_name",
    Theme = "@style/app_theme",
    ScreenOrientation = ScreenOrientation.Portrait, 
    MainLauncher = true)]
public class MainActivity : AppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_main);

        var startButton = FindViewById<Button>(Resource.Id.start_button)!;
        startButton.Click += OnStartButtonClick;
    }
    
    private void OnStartButtonClick(object? sender, EventArgs e)
    {
        var intent = new Intent(this, typeof(PasActivity));
        StartActivity(intent);
    }
}