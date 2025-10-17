using AutoAPI.Domain.Models;

namespace AutoAPI.API.Services.Generation;
public class EntityGeneratorService
{
    private readonly ITemplateRenderer _renderer;
    private readonly string _outputPath;
    public EntityGeneratorService(ITemplateRenderer renderer, string projectRoot)
    {
        _renderer = renderer;
        var solutionDirectory = Directory.GetParent(projectRoot)?.FullName;
        _outputPath = Path.Combine(solutionDirectory, "AutoAPI.Domain", "Entities");
        Directory.CreateDirectory(_outputPath);
    }

    public async Task GenerateEntitiesAsync(IEnumerable<ClassDefinition> classes)
    {
        var valueTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { "bool","byte","short","int","long","float","double","decimal","DateTime","Guid" };

        foreach (var cls in classes)
        {
            var model = new
            {
                class_name = cls.ClassName,
                properties = cls.Properties.Select(p =>
                {
                    var clrType = p.Type;

                    // IsNullable true ise hem value hem reference type için ? ekle
                    if (p.IsNullable)
                    {
                        clrType += "?";
                    }

                    return new
                    {
                        name = p.Name,
                        clr_type = clrType
                    };
                })
            };

            var code = await _renderer.RenderAsync("entity.scriban", model);
            var filePath = Path.Combine(_outputPath, $"{cls.ClassName}.cs");
            await File.WriteAllTextAsync(filePath, code);
        }
    }
}