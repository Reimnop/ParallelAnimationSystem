# Parallel Animation System 🔥

> The Parallel Animation System **(PAS)** is an app designed to emulate Project Arrhythmia at
> ⚡ **lightning speed** ⚡. It supports most of PA's animation features while being lightweight
> and extremely fast.

## Features

- ⚡ Emulates Project Arrhythmia's animation system
- 💬 Renders text objects
- 🔥 Multi-threaded animations
- 💅 High quality rendering
- 🗿 **Based as hell**

## Usage

### Dependencies

- [.NET 8](https://dotnet.microsoft.com/download/dotnet/8.0)

### Building

1. Clone the repository:
    ```sh
    git clone https://github.com/Reimnop/ParallelAnimationSystem.git --recursive
    ```
   
2. Navigate to the cloned folder and run the project:
    ```sh
    dotnet run --project ParallelAnimationSystem.Desktop
    ```

This will run ParallelAnimationSystem and also build the PAS executable file in `ParallelAnimationSystem\ParallelAnimationSystem.Desktop\bin`.
    
### Arguments

Parallel Animation System takes the following command line arguments:

| Argument                     | Default      | Description                                                          |
|------------------------------|--------------|----------------------------------------------------------------------|
| -b, --beatmap <beatmap>      | **required** | Path to the beatmap file (.lsb or .vgd)                              |
| -a, --audio <audio>          | **required** | Path to the audio file                                               |
| --vsync                      | True         | Enable VSync.                                                        |
| --workers <workers>          | 4            | Number of worker threads, set to -1 to use all available threads     |
| --seed <seed>                | -1           | Seed for the random number generator, set to -1 to use a random seed |
| --speed <speed>              | 1            | Sets the playback speed                                              |
| --backend <OpenGL\|OpenGLES> | OpenGL       | Sets the rendering backend to use                                    |
| --post-processing            | true         | Enable post-processing effects                                       |
| --text-rendering             | true         | Enable text rendering                                                |
| --version                    |              | Show version information                                             |
| -?, -h, --help               |              | Show help and usage information                                      |

Example usage:
 ```sh
 dotnet run --project ParallelAnimationSystem.Desktop -- -b level.vgd -a audio.ogg
 ```

Or, when running the executable directly:
```sh
 ParallelAnimationSystem.Desktop.exe -b level.vgd -a audio.ogg
 ```

## Troubleshooting

### Build errors after pulling

When updating your local repository, run `git pull --recurse-submodules` to ensure all submodules are updated when pulling changes.

If you encounter build errors after running git pull, run `git submodule update` to ensure submodules are updated.

### GPU compatibility

Certain GPUs, such as Intel integrated graphics, are not currently supported by the OpenGL backend.

### If your machine has multiple GPUs, and one of them is not Intel

Ensure PAS is configured to use the non-Intel GPU.
(For Windows, this is done by adding the PAS executable in the system Graphics settings and setting it to use "high performance".)

### If your machine only uses Intel GPU(s)

You can try running PAS with the OpenGL ES backend with `--backend opengles` to force PAS to use OpenGL ES.
**This mode is significantly slower, but should work on most systems.**

## Contributing

Contributions are welcome! Please feel free to submit issues, fork the repository, and send pull requests.

## License

This project is licensed under the GPLv3 License - see the [LICENSE](LICENSE) file for details.

## Acknowledgements

- [Project Arrhythmia](https://store.steampowered.com/app/440310/Project_Arrhythmia/)
- [OpenTK](https://opentk.net/)
- [msdfgen](https://github.com/Chlumsky/msdfgen)
- [BASS](http://www.un4seen.com/)
- Fonts provided by [Google Fonts](https://fonts.google.com/), [DaFont](https://www.dafont.com/), [Fontsource](https://fontsource.org/), and [Code2000](https://www.code2001.com/code2000_page.htm)
