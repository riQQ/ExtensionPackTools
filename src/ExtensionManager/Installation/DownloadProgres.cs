using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

using ExtensionManager.UI.Worker;
using ExtensionManager.VisualStudio;
namespace ExtensionManager.Installation;

internal sealed class DownloadProgres : IProgress<DownloadResult>
{
    private readonly IProgress<ProgressStep<InstallStep>> _uiProgress;
    private readonly int _initialCount;
    private int _remainingCount;
    private int _failedCount;

    public DownloadProgres(IProgress<ProgressStep<InstallStep>> uiProgress, int initialCount)
    {
        _uiProgress = uiProgress;
        _initialCount = initialCount;
        _remainingCount = initialCount;
        _failedCount = 0;
    }

    public void Report(DownloadResult value)
    {
        int remainingCount, failedCount;

        switch (value)
        {
            case DownloadResult.Success:
                remainingCount = Interlocked.Decrement(ref _remainingCount);
                failedCount = _failedCount;
                break;

            case DownloadResult.Failure:
                remainingCount = _remainingCount;
                failedCount = Interlocked.Increment(ref _failedCount);
                break;

            default:
                throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(DownloadResult));
        }

        var text = failedCount > 0
            ? $"Downloading {remainingCount} extensions, {failedCount} failed ..."
            : $"Downloading {remainingCount} extensions ...";

        var currentCount = _initialCount - remainingCount;
        var percentage = currentCount / (float)_initialCount;

        Debug.WriteLine($"====== {currentCount} - {_initialCount}");

        _uiProgress.Report(percentage, InstallStep.DownloadVsix);
        _ = VSFacade.StatusBar.ShowProgressAsync(text, currentCount, _initialCount);
    }
}
