using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ps5To6.Tools.Common;

public static class DependencyGraph
{
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Edges(
        IReadOnlyList<ProjectInfo> projects)
    {
        Dictionary<string, string> byPath = projects.ToDictionary(
            p => Norm(p.Path), p => p.Id, StringComparer.OrdinalIgnoreCase);

        var edges = new Dictionary<string, IReadOnlyList<string>>();
        foreach (ProjectInfo p in projects)
        {
            List<string> refs = p.ProjectReferencePaths
                .Select(Norm)
                .Where(byPath.ContainsKey)
                .Select(rp => byPath[rp])
                .ToList();
            edges[p.Id] = refs;
        }
        return edges;
    }

    public static IReadOnlyList<ProjectInfo> BottomUpOrder(IReadOnlyList<ProjectInfo> projects)
    {
        IReadOnlyDictionary<string, IReadOnlyList<string>> edges = Edges(projects);
        Dictionary<string, ProjectInfo> byId = projects.ToDictionary(p => p.Id);

        var ordered = new List<ProjectInfo>();
        var state = new Dictionary<string, int>(); // 0=unvisited,1=visiting,2=done

        void Visit(string id)
        {
            state.TryGetValue(id, out int s);
            if (s == 2) return;
            if (s == 1) throw new InvalidOperationException($"Project reference cycle at '{id}'.");
            state[id] = 1;
            foreach (string dep in edges[id]) Visit(dep);
            state[id] = 2;
            ordered.Add(byId[id]);
        }

        foreach (ProjectInfo p in projects) Visit(p.Id);
        return ordered; // dependencies emitted before dependents = bottom-up
    }

    private static string Norm(string path) => Path.GetFullPath(path);
}
