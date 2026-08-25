using FileMerger.Domain.Enums;
using FileMerger.Wpf.Features.Profile.Models;
using FileMerger.Wpf.Features.Profile.ViewModels;
using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Tests.Wpf.Features.Profile.ViewModels;

public sealed class ProfileListFiltersViewModelTests
{
    [Fact]
    public void Matches_Should_Show_All_Profiles_By_Default()
    {
        ProfileListFiltersViewModel filters = new();

        ProfileLibraryListItemViewModel userProfile =
            CreateProfile("User Profile", ProfileEntryKind.User);

        ProfileLibraryListItemViewModel builtInProfile =
            CreateProfile("Built-in Profile", ProfileEntryKind.BuiltIn);

        Assert.True(filters.Matches(userProfile));
        Assert.True(filters.Matches(builtInProfile));
        Assert.False(filters.HasActiveFilters);
    }

    [Fact]
    public void Matches_Should_Filter_User_Profiles()
    {
        ProfileListFiltersViewModel filters = new()
        {
            Kind = ProfileListKindFilterMode.User
        };

        ProfileLibraryListItemViewModel userProfile =
            CreateProfile("User Profile", ProfileEntryKind.User);

        ProfileLibraryListItemViewModel builtInProfile =
            CreateProfile("Built-in Profile", ProfileEntryKind.BuiltIn);

        Assert.True(filters.Matches(userProfile));
        Assert.False(filters.Matches(builtInProfile));
        Assert.True(filters.HasActiveFilters);
    }

    [Fact]
    public void Matches_Should_Filter_BuiltIn_Profiles()
    {
        ProfileListFiltersViewModel filters = new()
        {
            Kind = ProfileListKindFilterMode.BuiltIn
        };

        ProfileLibraryListItemViewModel userProfile =
            CreateProfile("User Profile", ProfileEntryKind.User);

        ProfileLibraryListItemViewModel builtInProfile =
            CreateProfile("Built-in Profile", ProfileEntryKind.BuiltIn);

        Assert.False(filters.Matches(userProfile));
        Assert.True(filters.Matches(builtInProfile));
        Assert.True(filters.HasActiveFilters);
    }

    [Fact]
    public void Matches_Should_Combine_Search_And_Kind()
    {
        ProfileListFiltersViewModel filters = new()
        {
            SearchText = "source",
            Kind = ProfileListKindFilterMode.BuiltIn
        };

        ProfileLibraryListItemViewModel userProfile =
            CreateProfile("Source User", ProfileEntryKind.User);

        ProfileLibraryListItemViewModel matchingBuiltInProfile =
            CreateProfile("Source Built-in", ProfileEntryKind.BuiltIn);

        ProfileLibraryListItemViewModel otherBuiltInProfile =
            CreateProfile("Docs Built-in", ProfileEntryKind.BuiltIn);

        Assert.False(filters.Matches(userProfile));
        Assert.True(filters.Matches(matchingBuiltInProfile));
        Assert.False(filters.Matches(otherBuiltInProfile));
    }

    [Fact]
    public void Reset_Should_Clear_Search_And_Kind_Filter()
    {
        ProfileListFiltersViewModel filters = new()
        {
            SearchText = "source",
            Kind = ProfileListKindFilterMode.BuiltIn
        };

        Assert.True(filters.HasActiveFilters);

        filters.Reset();

        Assert.Equal(string.Empty, filters.SearchText);
        Assert.Equal(ProfileListKindFilterMode.All, filters.Kind);
        Assert.False(filters.HasActiveFilters);
    }

    private static ProfileLibraryListItemViewModel CreateProfile(
        string name,
        ProfileEntryKind kind)
    {
        bool isBuiltIn = kind == ProfileEntryKind.BuiltIn;

        ProfileMetadataDto metadata = new(
            Id: Guid.NewGuid().ToString("N"),
            Name: name,
            Description: $"{name} description",
            CreatedAtUtc: DateTime.UtcNow,
            UpdatedAtUtc: DateTime.UtcNow,
            IsBuiltIn: isBuiltIn,
            IsReadOnly: isBuiltIn);

        ProfileLibraryEntry entry = new(
            Metadata: metadata,
            Profile: CreateProfileDto(),
            FilePath: kind == ProfileEntryKind.User
                ? $"{name}.filemerger.profile.json"
                : null,
            Kind: kind);

        return new ProfileLibraryListItemViewModel(entry);
    }

    private static WorkspaceProfileDto CreateProfileDto()
    {
        return new WorkspaceProfileDto(
            IncludeHeaderComment: false,
            IncludeFileSeparators: true,
            IncludeRelativePathInSeparator: true,
            TrimTrailingEmptyLines: true,
            RemoveUsingDirectives: false,
            FileTypes: [],
            LineEndingMode: LineEndingMode.Preserve,
            SortMode: SortMode.ByRelativePathAscending,
            InputEncodingMode: InputEncodingMode.Auto,
            PreferredInputEncodingName: null,
            FallbackInputEncodingName: "windows-1251");
    }
}