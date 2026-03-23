using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Domain.Entities;
using NoorLocator.Domain.Enums;
using NoorLocator.Infrastructure.Services.Centers;
using NoorLocator.UnitTests.TestHelpers;

namespace NoorLocator.UnitTests.Services.Centers;

public class CenterRequestServiceTests
{
    [Fact]
    public async Task CreateAsync_StoresPendingRequestAndWritesAuditEntry()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        dbContext.Users.Add(new User
        {
            Id = 7,
            Name = "Test User",
            Email = "user@test.local",
            PasswordHash = "hash",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = new CenterRequestService(dbContext, TestDbContextFactory.CreateAuditLogger(dbContext));

        var result = await service.CreateAsync(new CreateCenterRequestDto
        {
            Name = "New Test Center",
            Address = "Road 1",
            City = "Malmo",
            Country = "Sweden",
            Latitude = 55.6050m,
            Longitude = 13.0038m,
            Description = "Pending test request."
        }, 7);

        Assert.True(result.Succeeded);
        Assert.Equal(202, result.StatusCode);

        var request = Assert.Single(dbContext.CenterRequests);
        Assert.Equal(ModerationStatus.Pending, request.Status);
        Assert.Equal("New Test Center", request.Name);

        var auditLog = Assert.Single(dbContext.AuditLogs);
        Assert.Equal("CenterRequestSubmitted", auditLog.Action);
    }

    [Fact]
    public async Task CreateAsync_RejectsSimilarPublishedCenter()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        dbContext.Users.Add(new User
        {
            Id = 9,
            Name = "Test User",
            Email = "user2@test.local",
            PasswordHash = "hash",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        });
        dbContext.Centers.Add(new Center
        {
            Name = "Noor House",
            Address = "Existing Street",
            City = "Copenhagen",
            Country = "Denmark",
            Latitude = 55.6800m,
            Longitude = 12.5700m,
            Description = "Existing center."
        });
        await dbContext.SaveChangesAsync();

        var service = new CenterRequestService(dbContext, TestDbContextFactory.CreateAuditLogger(dbContext));

        var result = await service.CreateAsync(new CreateCenterRequestDto
        {
            Name = "Noor House",
            Address = "Candidate Street",
            City = "Copenhagen",
            Country = "Denmark",
            Latitude = 55.6810m,
            Longitude = 12.5710m,
            Description = "Should be rejected as a duplicate."
        }, 9);

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Empty(dbContext.CenterRequests);
    }
}
