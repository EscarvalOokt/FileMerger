using FileMerger.Domain.ValueObjects;
using FileMerger.Wpf.Features.Preview.State;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Session.ViewModels;

public sealed class SessionSettingsViewModel : ViewModelBase
{
    private string _outputPath = string.Empty;
    private string _sessionName = "Default Session";

    public string SessionName
    {
        get => _sessionName;
        set => SetProperty(ref _sessionName, value);
    }

    public string OutputPath
    {
        get => _outputPath;
        set => SetProperty(ref _outputPath, value);
    }

    public void LoadDefaults()
    {
        SessionName = "Default Session";
        OutputPath = string.Empty;
    }

    public OutputTarget BuildOutputTarget()
    {
        return new OutputTarget(OutputPath);
    }

    public PreviewSessionStateSnapshot BuildPreviewSessionSnapshot()
    {
        return new PreviewSessionStateSnapshot(SessionName, OutputPath);
    }
}