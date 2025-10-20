using AutoAPI.Core.Services;
using AutoAPI.Domain.Models;

namespace AutoAPI.Core.Generation;

public class AppDbContextGeneratorService
{
    private readonly ITemplateRenderer _renderer;
    private readonly string _contextFilePath;
    private readonly string _entitiesDirectoryPath; // Yeni: Entity'lerin yolunu tutacağız

    public AppDbContextGeneratorService(ITemplateRenderer renderer, string projectRoot)
    {
        _renderer = renderer;

        // KRİTİK DÜZELTME: EntityGeneratorService'deki başarılı mantığı kullan.
        // Bu, hem Docker (/src) hem de yerel ortamı (projectRoot) kapsar.
        var solutionRootPath = Directory.Exists("/src") ? "/src" :
            Directory.GetParent(projectRoot)?.FullName
                ?? throw new InvalidOperationException("Solution directory not found.");

        // 1️⃣ DÜZELTME: DbContext dosyasının yolu
        _contextFilePath = Path.Combine(solutionRootPath, "AutoAPI.Data", "Infrastructure", "AppDbContext.cs");

        // 2️⃣ DÜZELTME: Entity'leri okumak için Domain projesinin yolu
        _entitiesDirectoryPath = Path.Combine(solutionRootPath, "AutoAPI.Domain", "Entities");
    }

    public async Task GenerateAppDbContextAsync(List<ClassDefinition> definitions)
    {
        // 1️⃣ Tüm Entity'leri Domain klasöründen oku
        var allEntities = Directory.GetFiles(_entitiesDirectoryPath, "*.cs")
            .Select(Path.GetFileNameWithoutExtension)
            // Sadece C# dosya adlarını alıyoruz.
            .Where(name => name != null && name != "BaseEntity") // BaseEntity varsa hariç tut
            .Distinct()
            .ToList();

        // 2️⃣ Hata: Eğer appdbcontext.scriban'da DbSet'leri çoğul yapmak için 's' takısı 
        // kullanıyorsanız ('{{ e.name }}s'), burada da aynı formatı korumak gerekir.

        var templateModel = new
        {
            // Scriban'a gönderilen model artık tüm Entity dosyalarını içeriyor.
            entities = allEntities.Select(e => new { name = e }).ToList()
        };

        // 3️⃣ Şablonu çalıştır ve hedef yola yaz
        var output = await _renderer.RenderAsync("appdbcontext.scriban", templateModel);

        // KRİTİK: Dosya yazma işlemi başarılı olmalı.
        await File.WriteAllTextAsync(_contextFilePath, output);
    }
}