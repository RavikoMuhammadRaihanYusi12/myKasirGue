using Microsoft.EntityFrameworkCore;
using WarungApp.Core.Interfaces;

namespace WarungApp.Data;

public class MigrationService(AppDbContext db) : IMigrationService
{
    public Task MigrateAsync() => db.Database.MigrateAsync();
}
