using Courierly.Core.Domain;

namespace Courierly.Core.Tests;

public class ShipmentStatusTests
{
    [Fact]
    public void Parse_KnownStatusName_ReturnsEnumValue()
    {
        var parsed = Enum.Parse<ShipmentStatus>("InTransit");

        Assert.Equal(ShipmentStatus.InTransit, parsed);
    }
}
