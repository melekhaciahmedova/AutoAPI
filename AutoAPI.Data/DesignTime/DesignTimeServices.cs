using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace AutoAPI.Data.DesignTime;

public class DesignTimeServices : IDesignTimeServices
{
    public void ConfigureDesignTimeServices(IServiceCollection services)
    {
        // EF Core'un design-time servislerini DI'a yükle
        new EntityFrameworkDesignServicesBuilder(services)
            .TryAddCoreServices();
    }
}