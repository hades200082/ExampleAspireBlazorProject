using System.ComponentModel.DataAnnotations;
using Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Entities;

public sealed class Role : SimpleEntity<Ulid>
{
    [Required]
    [MinLength(3)]
    [MaxLength(200)]
    public required string Name { get; set; }
    
    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public bool Locked => Name == GlobalAdministrator;

    public IList<string> Permissions { get; set; } = [];

    public const string GlobalAdministrator = "GlobalAdministrator";
    public static readonly Ulid GlobalAdministratorId = Ulid.Parse("01J3FNSAXYKYDKB38EAC3AJ11M");
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasIndex(x => x.Name).IsUnique();
    }
}