using System;
using System.IO;
using Ps5To6.Tools.Common;

// Usage: ps5to6-report <runStatusJson> <outputMarkdown>
if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: ps5to6-report <runStatusJson> <outputMarkdown>");
    return 2;
}

RunStatus status = System.Text.Json.JsonSerializer.Deserialize<RunStatus>(
    File.ReadAllText(args[0]),
    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
    ?? throw new InvalidOperationException("Could not parse run status JSON.");

File.WriteAllText(args[1], RunReport.Render(status));
Console.WriteLine($"Wrote report -> {args[1]}");
return 0;
