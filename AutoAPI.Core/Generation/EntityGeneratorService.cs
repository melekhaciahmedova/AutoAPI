using AutoAPI.Core.Services;
using AutoAPI.Domain.Models;

namespace AutoAPI.Core.Generation;
public class EntityGeneratorService
{
    private readonly ITemplateRenderer _renderer;
    private readonly string _outputPath;

    public EntityGeneratorService(ITemplateRenderer renderer, string projectRoot)
    {
        _renderer = renderer;

        var basePath = Directory.Exists("/src") ? "/src" : projectRoot;

        _outputPath = Path.Combine(basePath, "AutoAPI.Domain", "Entities");

        Directory.CreateDirectory(_outputPath);
        Console.WriteLine($"📂 Entity output path: {_outputPath}");
    }

    public async Task GenerateEntitiesAsync(IEnumerable<ClassDefinition> classes)
    {
        foreach (var cls in classes)
        {
            var model = new
            {
                class_name = cls.ClassName,
                properties = cls.Properties.Select(p =>
                {
                    var clrType = p.Type;
                    if (p.IsNullable) clrType += "?";
                    return new { name = p.Name, clr_type = clrType };
                })
            };

            var code = await _renderer.RenderAsync("entity.scriban", model);
            var filePath = Path.Combine(_outputPath, $"{cls.ClassName}.cs");

            await File.WriteAllTextAsync(filePath, code);
            Console.WriteLine($"✅ Entity created: {filePath}");
        }
    }
}