using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

    [NotNull]
    private static string? EffectCompilerPath = "";

    private static FileSystemWatcher? EffectWatcher;

    private static string ModSource = "";

    #endregion

    #region Loading

    public override void Load()
    {
        ModSource = Mod.SourceFolder.Replace('\\', '/');

        EffectCompilerPath = Directory.GetFiles(ModSource, "*fxc.exe", SearchOption.AllDirectories)?[0];

        if (string.IsNullOrEmpty(EffectCompilerPath))
        {
            Mod.Logger.Info("'fxc.exe' not found! Effects will not be compiled!");
            return;
        }

        EffectCompilerPath = EffectCompilerPath.Replace('\\', '/');

        EffectWatcher = new(ModSource);

        foreach (string e in EffectExtensions)
            EffectWatcher.Filters.Add($"*{e}");

        EffectWatcher.Changed += EffectChanged;

        EffectWatcher.NotifyFilter = AllFilters;

        EffectWatcher.IncludeSubdirectories = true;
        EffectWatcher.EnableRaisingEvents = true;
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
            CompileShaderTask(EffectCompilerPath, effectPath, shortPath));
    }

    private async Task CompileShaderTask(string effectCompilerPath, string effectPath, string shortPath)
    {
        await Task.Yield();

        string outputEffect = Path.ChangeExtension(effectPath, ".fxc");

        ProcessStartInfo pInfo = new()
        {
            FileName = effectCompilerPath,
            Arguments = $"\"{effectPath}\" /T fx_2_0 /nologo /O2 /Fo \"{outputEffect}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = new();

        process.StartInfo = pInfo;

        process.ErrorDataReceived += (_, e) =>
            LogShaderCompilationError(e.Data ?? string.Empty);

        process.Start();

        process.BeginErrorReadLine();

        process.WaitForExit();

        if (process.ExitCode == 0)
            return;

        Mod.Logger.Warn($"Effect at {shortPath} could not be compiled! Exit code: {process.ExitCode}");
    }

    #endregion

    #region Logging

    private void LogShaderCompilationError(string error)
    {
        if (error.Length <= 0)
            return;

        if (!error.Contains("error"))
            return;

        Mod.Logger.Warn(error);
    }

    #endregion
}

#endif
