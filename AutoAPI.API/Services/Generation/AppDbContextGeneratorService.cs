using AutoAPI.Domain.Models;

namespace AutoAPI.API.Services.Generation;

public class AppDbContextGeneratorService
{
    private readonly ITemplateRenderer _renderer;
    private readonly string _contextFilePath;
    public AppDbContextGeneratorService(ITemplateRenderer renderer, string projectRoot)
    {
        _renderer = renderer;
        _contextFilePath = projectRoot;
        var solutionDirectory = Directory.GetParent(projectRoot)?.FullName;
        var outputDirectory = Path.Combine(solutionDirectory, "AutoAPI.Data", "Infrastructure");
        _contextFilePath = Path.Combine(outputDirectory, "AppDbContext.cs");
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
                if (line.TrimStart().StartsWith("public DbSet<"))
                {
                    var name = line.Split('<', '>')[1];
                    existingEntities.Add(name);
                }
            }
        }

        var newEntities = definitions.Select(d => d.ClassName).Except(existingEntities).ToList();
        existingEntities.AddRange(newEntities);

        var templateModel = new
        {
            entities = existingEntities.Select(e => new { name = e }).ToList()
        };

        var output = await _renderer.RenderAsync("appdbcontext.scriban", templateModel);

        await File.WriteAllTextAsync(contextPath, output);
    }
}