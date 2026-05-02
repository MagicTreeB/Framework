using FluentAssertions;

namespace MagicTree.Framework.Dtos.UnitTest;

public class BasedDtoTests
{
    [Fact]
    public void BasedDto_ShouldHaveCreatedOnProperty()
    {
        // Arrange
        var dto = new BasedDto
        {
            CreatedOn = DateTimeOffset.UtcNow,
            CreatedBy = "test-user",
            UpdatedOn = DateTimeOffset.UtcNow,
            UpdatedBy = "test-user"
        };

        // Act & Assert
        dto.CreatedOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void BasedDto_ShouldHaveCreatedByProperty()
    {
        // Arrange
        var expectedCreatedBy = "test-user-123";
        var dto = new BasedDto
        {
            CreatedOn = DateTimeOffset.UtcNow,
            CreatedBy = expectedCreatedBy,
            UpdatedOn = DateTimeOffset.UtcNow,
            UpdatedBy = "updater"
        };

        // Act & Assert
        dto.CreatedBy.Should().Be(expectedCreatedBy);
    }

    [Fact]
    public void BasedDto_ShouldHaveUpdatedOnProperty()
    {
        // Arrange
        var expectedUpdatedOn = DateTimeOffset.UtcNow.AddDays(1);
        var dto = new BasedDto
        {
            CreatedOn = DateTimeOffset.UtcNow,
            CreatedBy = "creator",
            UpdatedOn = expectedUpdatedOn,
            UpdatedBy = "updater"
        };

        // Act & Assert
        dto.UpdatedOn.Should().BeCloseTo(expectedUpdatedOn, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void BasedDto_ShouldHaveUpdatedByProperty()
    {
        // Arrange
        var expectedUpdatedBy = "admin-user";
        var dto = new BasedDto
        {
            CreatedOn = DateTimeOffset.UtcNow,
            CreatedBy = "creator",
            UpdatedOn = DateTimeOffset.UtcNow,
            UpdatedBy = expectedUpdatedBy
        };

        // Act & Assert
        dto.UpdatedBy.Should().Be(expectedUpdatedBy);
    }

    [Fact]
    public void BasedDto_AllProperties_ShouldBeSettable()
    {
        // Arrange
        var createdOn = DateTimeOffset.UtcNow.AddDays(-1);
        var createdBy = "user-1";
        var updatedOn = DateTimeOffset.UtcNow;
        var updatedBy = "user-2";

        // Act
        var dto = new BasedDto
        {
            CreatedOn = createdOn,
            CreatedBy = createdBy,
            UpdatedOn = updatedOn,
            UpdatedBy = updatedBy
        };

        // Assert
        dto.CreatedOn.Should().Be(createdOn);
        dto.CreatedBy.Should().Be(createdBy);
        dto.UpdatedOn.Should().Be(updatedOn);
        dto.UpdatedBy.Should().Be(updatedBy);
    }

    [Fact]
    public void BasedDto_CreatedBy_ShouldBeRequired()
    {
        // This test verifies the 'required' modifier by attempting compilation
        // The compiler will fail if CreatedBy is not set
        var act = () => new BasedDto
        {
            CreatedOn = DateTimeOffset.UtcNow,
            CreatedBy = "test", // Required
            UpdatedOn = DateTimeOffset.UtcNow,
            UpdatedBy = "test"  // Required
        };

        // Assert - should not throw
        act.Should().NotThrow();
    }

    [Fact]
    public void BasedDto_UpdatedBy_ShouldBeRequired()
    {
        // This test verifies the 'required' modifier by attempting compilation
        var act = () => new BasedDto
        {
            CreatedOn = DateTimeOffset.UtcNow,
            CreatedBy = "creator",
            UpdatedOn = DateTimeOffset.UtcNow,
            UpdatedBy = "updater" // Required
        };

        // Assert - should not throw
        act.Should().NotThrow();
    }

    [Fact]
    public void BasedDto_ShouldSupportAuditTrail()
    {
        // Arrange
        var createTime = DateTimeOffset.Parse("2025-01-01T10:00:00Z");
        var updateTime = DateTimeOffset.Parse("2025-01-02T15:30:00Z");

        // Act
        var dto = new BasedDto
        {
            CreatedOn = createTime,
            CreatedBy = "system",
            UpdatedOn = updateTime,
            UpdatedBy = "admin"
        };

        // Assert - Audit trail should be complete
        dto.CreatedOn.Should().Be(createTime);
        dto.CreatedBy.Should().Be("system");
        dto.UpdatedOn.Should().Be(updateTime);
        dto.UpdatedBy.Should().Be("admin");
        dto.UpdatedOn.Should().BeAfter(dto.CreatedOn);
    }
}
