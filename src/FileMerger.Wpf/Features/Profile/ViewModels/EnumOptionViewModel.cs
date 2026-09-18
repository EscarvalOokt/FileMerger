namespace FileMerger.Wpf.Features.Profile.ViewModels;

public sealed record EnumOptionViewModel<T>(T Value, string DisplayName) where T : struct, Enum;