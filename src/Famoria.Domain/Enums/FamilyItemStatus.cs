namespace Famoria.Domain.Enums;

public enum FamilyItemStatus
{
    New,
    //Eligible, // not used in the current implementation // it will be set by email filtering logic
    //Ignored, // not used in the current implementation // it will be set by email filtering logic
    Processed,
    Error
}
