#nullable enable

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using GameSphere_backend.Enums;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameSphere_backend.Tests.Authentication;

public sealed class UserIsolationTests : UserAccountTestBase, IClassFixture<PostgreSqlFixture>
{
    public UserIsolationTests(PostgreSqlFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task User_cannot_read_another_user_by_id()
    {
        var (owner, other) = await SeedUsersAsync();

        using var request = CreateAuthorizedRequest(HttpMethod.Get, $"/api/User/by-id/{other.Id}", owner);
        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task User_cannot_read_another_user_by_email()
    {
        var (owner, other) = await SeedUsersAsync();

        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/User/by-email/{Uri.EscapeDataString(other.Email)}",
            owner);
        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task User_cannot_update_another_user()
    {
        var (owner, other) = await SeedUsersAsync();

        using var request = CreateAuthorizedRequest(HttpMethod.Put, $"/api/User/{other.Id}", owner);
        request.Content = JsonContent.Create(new
        {
            firstName = "Unauthorized",
            lastName = "Update",
            email = other.Email,
            gender = (int)Gender.OUTRO,
            isActive = true,
        });

        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var context = CreateContext();
        var persistedUser = await context.Users.SingleAsync(user => user.Id == other.Id, TestContext.Current.CancellationToken);
        Assert.Equal("Other", persistedUser.FirstName);
    }

    [Fact]
    public async Task User_cannot_delete_another_user()
    {
        var (owner, other) = await SeedUsersAsync();

        using var request = CreateAuthorizedRequest(HttpMethod.Delete, $"/api/User/{other.Id}", owner);
        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var context = CreateContext();
        Assert.True(await context.Users.AnyAsync(user => user.Id == other.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task User_can_read_and_update_own_profile()
    {
        var (owner, _) = await SeedUsersAsync();

        using (var getRequest = CreateAuthorizedRequest(HttpMethod.Get, $"/api/User/by-id/{owner.Id}", owner))
        {
            var getResponse = await Client.SendAsync(getRequest, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        }

        using var updateRequest = CreateAuthorizedRequest(HttpMethod.Put, $"/api/User/{owner.Id}", owner);
        updateRequest.Content = JsonContent.Create(new
        {
            firstName = "Updated",
            lastName = "Owner",
            email = owner.Email,
            gender = (int)Gender.FEMININO,
            isActive = true,
        });

        var updateResponse = await Client.SendAsync(updateRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        await using var context = CreateContext();
        var persistedUser = await context.Users.SingleAsync(user => user.Id == owner.Id, TestContext.Current.CancellationToken);
        Assert.Equal("Updated", persistedUser.FirstName);
        Assert.Equal(Gender.FEMININO, persistedUser.Gender);
    }

    [Fact]
    public async Task Profile_update_accepts_an_omitted_last_name()
    {
        var (owner, _) = await SeedUsersAsync();

        using var updateRequest = CreateAuthorizedRequest(HttpMethod.Put, $"/api/User/{owner.Id}", owner);
        updateRequest.Content = JsonContent.Create(new
        {
            firstName = "Updated",
            email = owner.Email,
            gender = (int)Gender.FEMININO,
            isActive = true,
        });

        var updateResponse = await Client.SendAsync(updateRequest, TestContext.Current.CancellationToken);
        var body = await updateResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.True(updateResponse.StatusCode == HttpStatusCode.OK, body);
        await using var context = CreateContext();
        var persistedUser = await context.Users.SingleAsync(user => user.Id == owner.Id, TestContext.Current.CancellationToken);
        Assert.Equal("Updated", persistedUser.FirstName);
        Assert.Equal(owner.LastName, persistedUser.LastName);
    }

    [Fact]
    public async Task User_can_delete_own_account()
    {
        var (owner, _) = await SeedUsersAsync();

        using var request = CreateAuthorizedRequest(HttpMethod.Delete, $"/api/User/{owner.Id}", owner);
        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var context = CreateContext();
        Assert.False(await context.Users.AnyAsync(user => user.Id == owner.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Registration_does_not_accept_server_managed_fields()
    {
        var email = $"managed-fields-{Guid.NewGuid():N}@example.test";

        var response = await Client.PostAsJsonAsync("/api/User", new
        {
            id = 999999,
            firstName = "Managed",
            lastName = "Fields",
            email,
            password = "Registration-password-123",
            gender = (int)Gender.OUTRO,
            uid = "attacker-controlled-uid",
            image = "https://attacker.example/avatar.png",
            level = 99,
            totalPoints = 999999,
            isActive = false,
            registrationDate = "2099-01-01T00:00:00Z",
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await using var context = CreateContext();
        var user = await context.Users.SingleAsync(candidate => candidate.Email == email, TestContext.Current.CancellationToken);

        Assert.NotEqual(999999, user.Id);
        Assert.Equal(0, user.Level);
        Assert.Equal(0, user.TotalPoints);
        Assert.True(user.isActive);
        Assert.Null(user.UID);
        Assert.Null(user.Image);
        Assert.InRange(user.RegistrationDate, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task Profile_update_rejects_invalid_profile_without_persisting_changes()
    {
        var (owner, _) = await SeedUsersAsync();

        using var request = CreateAuthorizedRequest(HttpMethod.Put, $"/api/User/{owner.Id}", owner);
        request.Content = JsonContent.Create(new
        {
            firstName = " ",
            lastName = "Mutated",
            email = "invalid-email",
            gender = (int)Gender.FEMININO,
            image = "https://example.test/changed-avatar.png",
        });

        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var context = CreateContext();
        var persistedUser = await context.Users.SingleAsync(user => user.Id == owner.Id, TestContext.Current.CancellationToken);
        Assert.Equal(owner.FirstName, persistedUser.FirstName);
        Assert.Equal(owner.LastName, persistedUser.LastName);
        Assert.Equal(owner.Email, persistedUser.Email);
        Assert.Equal(owner.Gender, persistedUser.Gender);
        Assert.Null(persistedUser.Image);
    }
}
