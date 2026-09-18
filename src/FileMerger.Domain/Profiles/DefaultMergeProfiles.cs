using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Domain.Profiles
{
    public static class DefaultMergeProfiles
    {
        public static MergeProfile CreateDefault()
        {
            return new MergeProfile(
                name: "Default",
                generalOptions: new GeneralMergeOptions(
                    includeHeaderComment: false,
                    includeFileSeparators: true,
                    includeRelativePathInSeparator: true,
                    trimTrailingEmptyLines: true,
                    lineEndingMode: LineEndingMode.Preserve,
                    sortMode: SortMode.ByRelativePathAscending,
                    inputEncodingMode: InputEncodingMode.Auto,
                    preferredInputEncodingName: null,
                    fallbackInputEncodingName: "windows-1251"),
                fileTypes: KnownFileTypes.Default,
                filterRules: BuildDefaultFilterRules(),
                transformations:
                [
                    new ContentTransformationRule(
                        kind: TransformationKind.TrimTrailingEmptyLines,
                        order: 1,
                        isEnabled: true),

                    new ContentTransformationRule(
                        kind: TransformationKind.NormalizeLineEndings,
                        order: 2,
                        isEnabled: false)
                ]);
        }

        private static IReadOnlyCollection<FileFilterRule> BuildDefaultFilterRules()
        {
            return
            [
                new FileFilterRule(
                    mode: FilterMode.Exclude,
                    target: FilterTarget.DirectorySegment,
                    patternType: RulePatternType.Exact,
                    pattern: "bin",
                    description: "Exclude bin directory"),

                new FileFilterRule(
                    mode: FilterMode.Exclude,
                    target: FilterTarget.DirectorySegment,
                    patternType: RulePatternType.Exact,
                    pattern: "obj",
                    description: "Exclude obj directory"),

                new FileFilterRule(
                    mode: FilterMode.Exclude,
                    target: FilterTarget.FileName,
                    patternType: RulePatternType.Wildcard,
                    pattern: "*.Designer.cs",
                    description: "Exclude designer files"),

                new FileFilterRule(
                    mode: FilterMode.Exclude,
                    target: FilterTarget.FileName,
                    patternType: RulePatternType.Exact,
                    pattern: "AssemblyInfo.cs",
                    description: "Exclude AssemblyInfo")
            ];
        }
    }
}