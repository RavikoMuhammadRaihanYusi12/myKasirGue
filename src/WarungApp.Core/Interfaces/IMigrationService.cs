using WarungApp.Core.Common;
using WarungApp.Core.Entities;

namespace WarungApp.Core.Interfaces;

public interface IMigrationService
{
    Task MigrateAsync();
}
