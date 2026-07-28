namespace SiteYonetim.Application.DTOs.Apartments;

public sealed class BlockDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public sealed class ApartmentTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal BaseDues { get; set; }
    public decimal ArsaPayi { get; set; }
}

public sealed class ApartmentDto
{
    public Guid Id { get; set; }
    public Guid BlockId { get; set; }
    public string BlockName { get; set; } = string.Empty;
    public Guid? ApartmentTypeId { get; set; }
    public string? ApartmentTypeName { get; set; }
    public decimal MonthlyDues { get; set; }
    public string DoorNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public string? OwnerName { get; set; }
    public string? ResidentName { get; set; }
    public string? Phone { get; set; }
    public bool IsOccupied { get; set; }
}

public sealed class CreateApartmentRequest
{
    public Guid BlockId { get; set; }
    public Guid? ApartmentTypeId { get; set; }
    public decimal MonthlyDues { get; set; }
    public string DoorNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public string? OwnerFullName { get; set; }
    public string? OwnerPhone { get; set; }
    public string? OwnerTc { get; set; }
}

public sealed class CreateBlockRequest
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public sealed class CreateApartmentTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal BaseDues { get; set; }
    public decimal ArsaPayi { get; set; }
}

/// <summary>Aylık aidat grafiği için tek veri noktası.</summary>
public sealed class MonthlyDuesPoint
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public decimal Paid { get; set; }
}
