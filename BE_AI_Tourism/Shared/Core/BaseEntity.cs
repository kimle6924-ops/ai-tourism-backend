namespace BE_AI_Tourism.Shared.Core;

// Base entity for all database models
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
