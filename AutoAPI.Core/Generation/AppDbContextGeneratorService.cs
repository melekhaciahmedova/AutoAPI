using AutoAPI.Core.Services;
using AutoAPI.Domain.Models;
using System.Text.RegularExpressions;

namespace AutoAPI.Core.Generation;

public class AppDbContextGeneratorService
{
    private readonly ITemplateRenderer _renderer;
    private readonly string _contextFilePath;

    public AppDbContextGeneratorService(ITemplateRenderer renderer, string projectRoot)
    {
        _renderer = renderer;

        var solutionDirectory = Directory.GetParent(projectRoot)?.FullName
            ?? throw new InvalidOperationException("Solution directory not found.");

        _contextFilePath = Path.Combine(solutionDirectory, "AutoAPI.Data", "Infrastructure", "AppDbContext.cs");
    }

    public async Task GenerateAppDbContextAsync(List<ClassDefinition> definitions)
    {
        var contextPath = _contextFilePath;
        var existingEntities = new List<string>();

        if (File.Exists(contextPath))
        {
            var lines = await File.ReadAllLinesAsync(contextPath);
            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"DbSet<\s*(\w+)\s*>");
                if (match.Success)
                    existingEntities.Add(match.Groups[1].Value);
            }
        }

        var comparer = StringComparer.OrdinalIgnoreCase;
        var newEntities = definitions.Select(d => d.ClassName)
            .Except(existingEntities, comparer)
            .ToList();

        existingEntities.AddRange(newEntities);

        var templateModel = new
        {
            entities = existingEntities.Select(e => new { name = e }).ToList()
        };

        var output = await _renderer.RenderAsync("appdbcontext.scriban", templateModel);
        await File.WriteAllTextAsync(contextPath, output);
    }
}