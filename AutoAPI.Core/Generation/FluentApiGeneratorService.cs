using AutoAPI.Core.Services;
using AutoAPI.Domain.Models;

namespace AutoAPI.Core.Generation;
public class FluentApiGeneratorService
{
    private readonly ITemplateRenderer _renderer;
    private readonly string _outputPath;

    public FluentApiGeneratorService(ITemplateRenderer renderer, string projectRoot)
    {
        _renderer = renderer;
        var basePath = Directory.Exists("/src") ? "/src" : projectRoot;
        _outputPath = Path.Combine(basePath, "AutoAPI.Data", "Infrastructure", "Configurations");
        Directory.CreateDirectory(_outputPath);
        Console.WriteLine($"Fluent API output path: {_outputPath}");
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
            Console.WriteLine($"Fluent API configuration created: {filePath}");
        }
    }
}