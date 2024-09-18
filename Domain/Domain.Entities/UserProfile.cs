using System.ComponentModel.DataAnnotations;
using Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Entities;

public sealed class UserProfile : SimpleEntity<string>
{
    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public required string Email { get; set; }
    
    [Required]
    [MinLength(3)]
    [MaxLength(200)]
    public required string Name { get; set; }

    public IList<Role> Roles { get; set; } = [];

    public DateTime? DeletedAt { get; set; }
}

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.HasMany<Role>(x => x.Roles).WithMany();
    }
}
