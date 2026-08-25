using FileMerger.Domain.ValueObjects;

namespace FileMerger.Domain.Entities
{
    public sealed record MergeSession
    {
        public MergeSession(
            Guid id,
            string name,
            IReadOnlyCollection<MergeSource> sources,
            MergeProfile profile,
            OutputTarget outputTarget,
            IReadOnlyCollection<InputFile>? files = null,
            IReadOnlyCollection<ValidationIssue>? validationIssues = null,
            MergeOutput? lastOutput = null)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Session id cannot be empty.", nameof(id));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Session name cannot be empty.", nameof(name));

            ArgumentNullException.ThrowIfNull(sources);
            ArgumentNullException.ThrowIfNull(profile);
            ArgumentNullException.ThrowIfNull(outputTarget);

            Id = id;
            Name = name;
            Sources = sources;
            Profile = profile;
            OutputTarget = outputTarget;
            Files = files ?? [];
            ValidationIssues = validationIssues ?? [];
            LastOutput = lastOutput;
        }

        public Guid Id { get; }
        public string Name { get; }
        public IReadOnlyCollection<MergeSource> Sources { get; }
        public MergeProfile Profile { get; }
        public OutputTarget OutputTarget { get; }
        public IReadOnlyCollection<InputFile> Files { get; init; }
        public IReadOnlyCollection<ValidationIssue> ValidationIssues { get; init; }
        public MergeOutput? LastOutput { get; init; }

        public MergeSession WithFiles(IReadOnlyCollection<InputFile> files)
        {
            ArgumentNullException.ThrowIfNull(files);
            return this with { Files = files };
        }

        public MergeSession WithValidationIssues(IReadOnlyCollection<ValidationIssue> issues)
        {
            ArgumentNullException.ThrowIfNull(issues);
            return this with { ValidationIssues = issues };
        }

        public MergeSession WithLastOutput(MergeOutput output)
        {
            ArgumentNullException.ThrowIfNull(output);
            return this with { LastOutput = output };
        }
    }
}