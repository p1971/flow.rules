using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using RulesEngine.Models;

namespace FlowRules.Benchmarks;

internal static class WorkflowLoader
{
    public static IReadOnlyList<Workflow> LoadWorkflows(string fileName)
    {
        string fileData = File.ReadAllText(FindWorkflow(fileName));
        return JsonSerializer.Deserialize<List<Workflow>>(fileData)
            ?? throw new InvalidOperationException($"Unable to deserialize RulesEngine workflows from {fileName}.");
    }

    private static string FindWorkflow(string fileName)
    {
        string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), fileName, SearchOption.AllDirectories);
        return files.Length == 0
            ? throw new InvalidOperationException($"{fileName} not found.")
            : files[0];
    }
}
