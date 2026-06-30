using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ps5To6.Tools.Common;

// Usage: ps5to6-scaffold-project <type> <packagesJson> <outputCsprojPath>
//   <type>: Service | RichClient | PublishingService | Configuration
//   <packagesJson>: JSON array of {"id":"...","version":"..."}
if (args.Length != 3 || !Enum.TryParse(args[0], out PsProjectType type))
{
    Console.Error.WriteLine("Usage: ps5to6-scaffold-project <Service|RichClient|PublishingService|Configuration> <packagesJson> <outputCsprojPath>");
    return 2;
}

var pkgs = System.Text.Json.JsonSerializer.Deserialize<List<PkgDto>>(File.ReadAllText(args[1])) ?? new();
var spec = new ScaffoldSpec(type, pkgs.Select(p => (p.Id, p.Version)).ToList());
File.WriteAllText(args[2], CsprojScaffolder.Build(spec));
Console.WriteLine($"Scaffolded {type} csproj -> {args[2]}");
return 0;

record PkgDto(string Id, string Version);
