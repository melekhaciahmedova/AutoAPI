using Scriban;

namespace AutoAPI.Core.Services;
public interface ITemplateRenderer
{
    Task<string> RenderAsync(string templateName, object model);
}
public class TemplateRenderer(string? templateRoot = null) : ITemplateRenderer
{
    private readonly string _templateRoot = templateRoot ?? Path.Combine(AppContext.BaseDirectory, "Templates");
    public async Task<string> RenderAsync(string templateName, object model)
    {
        var templatePath = Path.Combine(_templateRoot, templateName);
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template not found: {templatePath}");

        var templateText = await File.ReadAllTextAsync(templatePath);
        var template = Template.Parse(templateText);

        if (template.HasErrors)
            throw new InvalidOperationException($"Template parse errors: {string.Join(", ", template.Messages)}");

        return template.Render(model, member => member.Name);
    }
}