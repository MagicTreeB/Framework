using MagicTree.Framework.Share;

namespace MagicTree.Framework.Entity.Entities;

public class BaseEntity<T> : FlagMigrationEntity
{
    #region Constructors

    public BaseEntity(T id, string CreatedBy, string CreatedByName)
    {
        Id = id;
        SetCreated(CreatedBy, CreatedByName);
    }
    #endregion

    #region Properties
    public T Id { get; set; }

    public DateTimeOffset CreatedOn { get; set; }
    public string CreatedBy { get; set; } = ShareKey.SystemUser;
    public string CreatedByName { get; set; } = ShareKey.SystemUser;

    public DateTimeOffset? UpdatedOn { get; set; } = null;
    public string? UpdatedBy { get; set; } = null;
    public string? UpdatedByName { get; set; } = null;

    public bool IsDeleted { get; set; } = false;

    #endregion

    #region Methods

    public void SetCreated(string userId, string userName)
    {
        CreatedOn = DateTimeOffset.UtcNow;
        CreatedBy = userId;
        CreatedByName = userName;
    }

    public void SetUpdated(string userId)
    {
        UpdatedOn = DateTimeOffset.UtcNow;
        UpdatedBy = userId;
    }

    public void SoftDelete(string userId)
    {
        IsDeleted = true;
        SetUpdated(userId);
    }

    public void Restore(string userId)
    {
        IsDeleted = false;
        SetUpdated(userId);
    }
    #endregion
}
