using Shared.Enums;

namespace Infrastructure.EntityFrameworkCore;

public class DatabaseOptions
{
    public virtual required DatabaseProviders DatabaseProvider { get; set; }
    public virtual required string ConnectionStringName { get; set; }
}