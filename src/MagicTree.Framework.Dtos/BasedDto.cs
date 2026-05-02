
namespace MagicTree.Framework.Dtos;

public class BasedDto
{
    public DateTimeOffset? CreatedOn { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }

    public DateTimeOffset? UpdatedOn { get; set; }
    public string? UpdatedBy { get; set; }
}
