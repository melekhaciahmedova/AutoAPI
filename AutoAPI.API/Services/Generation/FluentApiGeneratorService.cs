using AutoAPI.Domain.Models;

namespace AutoAPI.API.Services.Generation;
public class FluentApiGeneratorService
{
    private readonly ITemplateRenderer _renderer;
    private readonly string _outputPath;
    public FluentApiGeneratorService(ITemplateRenderer renderer, string projectRoot)
    {
        _renderer = renderer;
        var solutionDirectory = Directory.GetParent(projectRoot)?.FullName;
        _outputPath = Path.Combine(solutionDirectory, "AutoAPI.Data", "Infrastructure", "Configurations");
        Directory.CreateDirectory(_outputPath);
    }

    public async Task GenerateFluentConfigurationsAsync(IEnumerable<ClassDefinition> classes)
    {
        foreach (var cls in classes)
        {
            var model = new
            {
                class_name = cls.ClassName,
                properties = cls.Properties.Select(p => new
                {
                    name = p.Name,
                    isRequired = p.IsRequired || p.IsKey || !p.IsNullable,
                    isNullable = p.IsNullable,
                    isKey = p.IsKey,
                    maxLength = p.MaxLength,
                    columnType = p.ColumnType
                })
            };

            var code = await _renderer.RenderAsync("configuration.scriban", model);
            var filePath = Path.Combine(_outputPath, $"{cls.ClassName}EntityConfiguration.cs");
            await File.WriteAllTextAsync(filePath, code);
        }
    }
}