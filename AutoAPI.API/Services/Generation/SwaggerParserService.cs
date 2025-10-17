using AutoAPI.Domain.Models;
using Microsoft.OpenApi.Readers;

namespace AutoAPI.API.Services.Generation;

public class SwaggerParserService
{
    public async Task<List<ClassDefinition>> ParseAsync(string swaggerFilePath)
    {
        using var stream = File.OpenRead(swaggerFilePath);
        var openApiDoc = new OpenApiStreamReader().Read(stream, out var diagnostic);
        var classes = new List<ClassDefinition>();

        foreach (var schema in openApiDoc.Components.Schemas)
        {
            var classDef = ConvertSchemaToClassDefinition(schema.Key, schema.Value);
            classes.Add(classDef);
        }

        return classes;
    }

    private ClassDefinition ConvertSchemaToClassDefinition(string className, Microsoft.OpenApi.Models.OpenApiSchema schema)
    {
        var classDef = new ClassDefinition { ClassName = className };

        foreach (var prop in schema.Properties)
        {
            var field = new PropertyDefinition
            {
                Name = prop.Key,
                Type = MapSwaggerType(prop.Value.Type, prop.Value.Format),
                IsNullable = prop.Value.Nullable,
                IsRequired = schema.Required.Contains(prop.Key),
                IsKey = prop.Value.Extensions.ContainsKey("x-key")
            };
            classDef.Properties.Add(field);
        }

        return classDef;
    }

    private static string MapSwaggerType(string? swaggerType, string? format)
    {
        return (swaggerType, format) switch
        {
            ("integer", "int64") => "long",
            ("integer", _) => "int",
            ("number", "float") => "float",
            ("number", "double") => "double",
            ("number", _) => "decimal",
            ("boolean", _) => "bool",
            ("string", "date-time") => "DateTime",
            ("string", _) => "string",
            _ => "object"
        };
    }
}