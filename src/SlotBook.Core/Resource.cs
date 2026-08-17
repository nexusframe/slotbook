namespace SlotBook.Core;

public class Resource
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public ResourceKind Kind { get; set; }

    public bool IsActive { get; set; } = true;
}
