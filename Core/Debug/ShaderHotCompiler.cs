using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Terraria.ModLoader;
using static System.IO.WatcherChangeTypes;

namespace ZensSky.Core.Debug;

#if DEBUG

[Autoload(Side = ModSide.Client)]
public sealed class ShaderHotCompiler : ModSystem
{
    #region Private Fields

    private const NotifyFilters AllFilters =
        NotifyFilters.FileName |
        NotifyFilters.DirectoryName |
        NotifyFilters.Attributes |
        NotifyFilters.Size |
        NotifyFilters.LastWrite |
        NotifyFilters.LastAccess |
        NotifyFilters.CreationTime |
        NotifyFilters.Security;

    private static readonly string[] EffectExtensions = [".fx", ".hlsl"];

    private static string FXCPath = "";

    private static FileSystemWatcher? EffectWatcher;

    private static string ModSource = "";

    #endregion

    #region Loading

    public override void Load()
    {
        try
        {
            ModSource = Mod.SourceFolder.Replace('\\', '/');

            string[] paths = Directory.GetFiles(ModSource, "*fxc.exe", SearchOption.AllDirectories);

            if (paths.Length <= 0)
            {
                Mod.Logger.Info("'fxc.exe' not found! Effects will not be compiled!");
                return;
            }

            FXCPath = paths[0].Replace('\\', '/');

            EffectWatcher = new(ModSource);

            foreach (string e in EffectExtensions)
                EffectWatcher.Filters.Add($"*{e}");

            EffectWatcher.Changed += EffectChanged;

            EffectWatcher.NotifyFilter = AllFilters;

            EffectWatcher.IncludeSubdirectories = true;
            EffectWatcher.EnableRaisingEvents = true;
        }
        catch (Exception e)
        {
            Mod.Logger.Warn($"Unable to load Shader Hot-Compiler! - {e}");
        }
    }

    public override void Unload() =>
        EffectWatcher?.Dispose();

    #endregion

    #region Effect Compilation

    private void EffectChanged(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType.HasFlag(Created))
            return;

        string effectPath = e.FullPath.Replace('\\', '/');

        string shortPath = Path.GetRelativePath(ModSource, effectPath);

        shortPath = Path.ChangeExtension(shortPath, null).Replace('\\', '/');

        if (e.ChangeType.HasFlag(Deleted) ||
            e.ChangeType.HasFlag(Renamed))
        {
            Mod.Logger.Warn($"Effect at {shortPath} was removed or renamed!");
            return;
        }

        Task.Run(() =>
            CompileShaderTask(FXCPath, effectPath, shortPath));
    }

    private async Task CompileShaderTask(string executable, string effectPath, string shortPath)
    {
            // Prevent alledged issues with temp files.
        await Task.Delay(10);

        string wineArgument = "";

        string outputEffect = Path.ChangeExtension(effectPath, ".fxc");

            // TODO: Properly test the below.
        if (OperatingSystem.IsLinux())
            HandleWineCompilation(ref executable, ref wineArgument, ref effectPath, ref outputEffect);

        ProcessStartInfo pInfo = new()
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            FileName = executable,
            Arguments = $"{wineArgument} \"{effectPath}\" /T fx_2_0 /nologo /O2 /Fo \"{outputEffect}\"",
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using Process process = new();

        process.StartInfo = pInfo;

        process.ErrorDataReceived += (_, e) =>
            LogShaderCompilationError(e.Data ?? string.Empty, effectPath, shortPath);

        process.Start();

        process.BeginErrorReadLine();

        process.WaitForExit();

        if (process.ExitCode == 0)
            return;

        Mod.Logger.Warn($"Effect at {shortPath} could not be compiled! Exit code: {process.ExitCode}");
    }

    #endregion

    #region Linux

    private void HandleWineCompilation(ref string executable, ref string wineArgument, ref string effectPath, ref string outputEffect)
    {
        ProcessStartInfo pInfo = new()
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            FileName = "/bin/bash",
            Arguments = "-c \"command -v wine\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using Process process = new();

        process.StartInfo = pInfo;
        process.Start();

        process.WaitForExit();

        string error = process.StandardError.ReadToEnd();
        string output = process.StandardOutput.ReadToEnd();

        if (!string.IsNullOrEmpty(error))
            Mod.Logger.Warn($"Error during WINE call converting {error}");

        if (string.IsNullOrEmpty(output))
            Mod.Logger.Warn($"Could not find WINE; maybe try installing it from your package manager?");

        wineArgument = executable;
        executable = output.Trim();

        WinePathConversion(ref effectPath);
        WinePathConversion(ref outputEffect);
    }

    private void WinePathConversion(ref string path)
    {
        ProcessStartInfo pInfo = new()
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            FileName = "/bin/bash",
            Arguments = $"-c \"winepath --windows '{path}'\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using Process process = new();

        process.StartInfo = pInfo;
        process.Start();

        process.WaitForExit();

        string error = process.StandardError.ReadToEnd();
        string output = process.StandardOutput.ReadToEnd();

        if (string.IsNullOrEmpty(output))
            Mod.Logger.Warn($"Error converting path \"{path}\" using WINE; {error}");

        path = output.Trim();
    }

    #endregion

    #region Logging

    private void LogShaderCompilationError(string error, string effectPath, string shortPath)
    {
        if (error.Length <= 0)
            return;

        error = error.Replace(effectPath, string.Empty);

        if (!error.Contains("error"))
            return;

        Mod.Logger.Warn($"{shortPath}: {error}");
    }

    #endregion
}

#endif
