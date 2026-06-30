using System.Collections.Generic;

namespace Ps5To6.Tools.Common;

public record PackageRef(string Id, string? Version, bool IsMicrosoftOrSystem);

public record ProjectInfo(
    string Id,
    string Path,
    IReadOnlyList<string> TargetFrameworks,
    bool IsSdkStyle,
    string PackageStyle,
    string? OutputType,
    bool IsDependencyOnly,
    IReadOnlyList<PackageRef> Packages,
    IReadOnlyList<string> ProjectReferencePaths);
