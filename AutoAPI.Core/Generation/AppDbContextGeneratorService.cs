using AutoAPI.Core.Services;
using AutoAPI.Domain.Models;

namespace AutoAPI.Core.Generation;
public class AppDbContextGeneratorService
{
    private readonly ITemplateRenderer _renderer;
    private readonly string _contextFilePath;
    private readonly string _entitiesDirectoryPath;
    public AppDbContextGeneratorService(ITemplateRenderer renderer, string projectRoot)
    {
        _renderer = renderer;

        var solutionRootPath = Directory.Exists("/src") ? "/src" :
            Directory.GetParent(projectRoot)?.FullName
                ?? throw new InvalidOperationException("Solution directory not found.");
        _contextFilePath = Path.Combine(solutionRootPath, "AutoAPI.Data", "Infrastructure", "AppDbContext.cs");
        _entitiesDirectoryPath = Path.Combine(solutionRootPath, "AutoAPI.Domain", "Entities");
    }

    public async Task GenerateAppDbContextAsync(List<ClassDefinition> definitions)
    {
        var allEntities = Directory.GetFiles(_entitiesDirectoryPath, "*.cs")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name != null && name != "BaseEntity")
            .Distinct()
            .ToList();

        var templateModel = new
        {
            entities = allEntities.Select(e => new { name = e }).ToList()
        };

        var output = await _renderer.RenderAsync("appdbcontext.scriban", templateModel);
        await File.WriteAllTextAsync(_contextFilePath, output);
    }
}