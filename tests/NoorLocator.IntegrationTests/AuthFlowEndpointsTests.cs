using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NoorLocator.Infrastructure.Persistence;

namespace NoorLocator.IntegrationTests;

public class AuthFlowEndpointsTests
{
    [Fact]
    public async Task Register_VerifyEmail_And_AllowLoginAfterVerification()
    {
        using var factory = new NoorLocatorWebApplicationFactory();
        var recorder = IntegrationTestSupport.GetEmailRecorder(factory);
        recorder.Clear();
        using var client = factory.CreateClient();

        var email = $"verify-{Guid.NewGuid():N}@test.local";
        const string password = "Secure123!";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Verification Flow User",
            email,
            password
        });

        var registerPayload = await IntegrationTestSupport.ReadEnvelopeAsync<AuthPayload>(registerResponse, HttpStatusCode.Created);

        Assert.NotNull(registerPayload.Data);
        Assert.Equal("Please check your email to verify your account.", registerPayload.Message);
        Assert.False(registerPayload.Data!.User!.IsEmailVerified);
        Assert.True(string.IsNullOrWhiteSpace(registerPayload.Data.Token));
        Assert.True(string.IsNullOrWhiteSpace(registerPayload.Data.RefreshToken));

        var verificationEmail = Assert.Single(recorder.Snapshot());
        Assert.Equal("noorlocator@gmail.com", verificationEmail.FromEmail);
        Assert.Equal(email, verificationEmail.ToEmail);
        var verificationToken = IntegrationTestSupport.ExtractToken(verificationEmail);

        var blockedLoginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });
        var blockedLoginPayload = await IntegrationTestSupport.ReadEnvelopeAsync<AuthPayload>(blockedLoginResponse, HttpStatusCode.Forbidden);
        Assert.Equal("Please verify your email before signing in.", blockedLoginPayload.Message);

        var verifyResponse = await client.GetAsync($"/api/auth/verify-email?token={Uri.EscapeDataString(verificationToken)}");
        var verifyPayload = await IntegrationTestSupport.ReadEnvelopeAsync<VerifyEmailPayload>(verifyResponse, HttpStatusCode.OK);

        Assert.NotNull(verifyPayload.Data);
        Assert.Equal("verified", verifyPayload.Data!.Status);
        Assert.Equal(email, verifyPayload.Data.Email);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<NoorLocatorDbContext>();
            var user = await dbContext.Users.SingleAsync(currentUser => currentUser.Email == email);
            Assert.True(user.IsEmailVerified);
            Assert.Null(user.EmailVerificationTokenHash);
            Assert.Null(user.EmailVerificationTokenExpiresAtUtc);
        }

        var loginPayload = await IntegrationTestSupport.LoginAsync(client, email, password);
        Assert.True(loginPayload.User!.IsEmailVerified);
        Assert.Equal(email, loginPayload.User.Email);
    }

    [Fact]
    public async Task VerifyEmail_ExpiredAndInvalidTokens_AreHandled_And_ResendWorks()
    {
        using var factory = new NoorLocatorWebApplicationFactory();
        var recorder = IntegrationTestSupport.GetEmailRecorder(factory);
        recorder.Clear();
        using var client = factory.CreateClient();

        var email = $"expired-{Guid.NewGuid():N}@test.local";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Expired Verification User",
            email,
            password = "Secure123!"
        });
        await IntegrationTestSupport.ReadEnvelopeAsync<AuthPayload>(registerResponse, HttpStatusCode.Created);

        var originalVerificationEmail = Assert.Single(recorder.Snapshot());
        var originalToken = IntegrationTestSupport.ExtractToken(originalVerificationEmail);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<NoorLocatorDbContext>();
            var user = await dbContext.Users.SingleAsync(currentUser => currentUser.Email == email);
            user.EmailVerificationTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5);
            await dbContext.SaveChangesAsync();
        }

        var expiredVerifyResponse = await client.GetAsync($"/api/auth/verify-email?token={Uri.EscapeDataString(originalToken)}");
        var expiredVerifyPayload = await IntegrationTestSupport.ReadEnvelopeAsync<VerifyEmailPayload>(expiredVerifyResponse, HttpStatusCode.Gone);

        Assert.NotNull(expiredVerifyPayload.Data);
        Assert.Equal("expired", expiredVerifyPayload.Data!.Status);
        Assert.Equal(email, expiredVerifyPayload.Data.Email);

        var invalidVerifyResponse = await client.GetAsync("/api/auth/verify-email?token=this-is-not-valid");
        var invalidVerifyPayload = await IntegrationTestSupport.ReadEnvelopeAsync<VerifyEmailPayload>(invalidVerifyResponse, HttpStatusCode.BadRequest);

        Assert.NotNull(invalidVerifyPayload.Data);
        Assert.Equal("invalid", invalidVerifyPayload.Data!.Status);

        var resendResponse = await client.PostAsJsonAsync("/api/auth/resend-verification-email", new
        {
            email
        });
        var resendPayload = await IntegrationTestSupport.ReadEnvelopeAsync<object?>(resendResponse, HttpStatusCode.OK);
        Assert.Equal("If an account needs verification, a new verification email has been sent.", resendPayload.Message);

        var allMessages = recorder.Snapshot().OrderBy(message => message.CreatedAtUtc).ToArray();
        Assert.Equal(2, allMessages.Length);
        var resentVerificationEmail = allMessages[^1];
        Assert.Equal("noorlocator@gmail.com", resentVerificationEmail.FromEmail);
        Assert.Equal(email, resentVerificationEmail.ToEmail);

        var resentToken = IntegrationTestSupport.ExtractToken(resentVerificationEmail);
        var resentVerifyResponse = await client.GetAsync($"/api/auth/verify-email?token={Uri.EscapeDataString(resentToken)}");
        var resentVerifyPayload = await IntegrationTestSupport.ReadEnvelopeAsync<VerifyEmailPayload>(resentVerifyResponse, HttpStatusCode.OK);
        Assert.Equal("verified", resentVerifyPayload.Data!.Status);
    }

    [Fact]
    public async Task ProfileEmailChange_MakesSessionUnverified_And_BlocksVerifiedEndpointsUntilReverified()
    {
        using var factory = new NoorLocatorWebApplicationFactory();
        var recorder = IntegrationTestSupport.GetEmailRecorder(factory);
        recorder.Clear();
        using var client = factory.CreateClient();

        var auth = await IntegrationTestSupport.LoginAsync(client, "user@test.local", "User123!Pass");
        IntegrationTestSupport.Authorize(client, auth.Token);

        var initialNotificationResponse = await client.GetAsync("/api/notifications/unread-count");
        await IntegrationTestSupport.ReadEnvelopeAsync<UnreadNotificationCountPayload>(initialNotificationResponse, HttpStatusCode.OK);

        var updatedEmail = $"changed-{Guid.NewGuid():N}@test.local";
        var updateProfileResponse = await client.PutAsJsonAsync("/api/profile/me", new
        {
            name = "Integration User Updated",
            email = updatedEmail
        });
        var updateProfilePayload = await IntegrationTestSupport.ReadEnvelopeAsync<ProfileDetailsPayload>(updateProfileResponse, HttpStatusCode.OK);

        Assert.NotNull(updateProfilePayload.Data);
        Assert.False(updateProfilePayload.Data!.IsEmailVerified);
        Assert.Equal(updatedEmail, updateProfilePayload.Data.Email);

        var blockedNotificationResponse = await client.GetAsync("/api/notifications/unread-count");
        Assert.Equal(HttpStatusCode.Forbidden, blockedNotificationResponse.StatusCode);

        var profileResponse = await client.GetAsync("/api/profile/me");
        var profilePayload = await IntegrationTestSupport.ReadEnvelopeAsync<ProfileDetailsPayload>(profileResponse, HttpStatusCode.OK);
        Assert.False(profilePayload.Data!.IsEmailVerified);

        var verificationEmail = Assert.Single(recorder.Snapshot());
        Assert.Equal("noorlocator@gmail.com", verificationEmail.FromEmail);
        Assert.Equal(updatedEmail, verificationEmail.ToEmail);
        var verificationToken = IntegrationTestSupport.ExtractToken(verificationEmail);

        var verifyResponse = await client.GetAsync($"/api/auth/verify-email?token={Uri.EscapeDataString(verificationToken)}");
        var verifyPayload = await IntegrationTestSupport.ReadEnvelopeAsync<VerifyEmailPayload>(verifyResponse, HttpStatusCode.OK);
        Assert.Equal("verified", verifyPayload.Data!.Status);

        var unblockedNotificationResponse = await client.GetAsync("/api/notifications/unread-count");
        await IntegrationTestSupport.ReadEnvelopeAsync<UnreadNotificationCountPayload>(unblockedNotificationResponse, HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_ResetPassword_RotatesCredentials_ExpiresTokens_And_DoesNotRevealAccounts()
    {
        using var factory = new NoorLocatorWebApplicationFactory();
        var recorder = IntegrationTestSupport.GetEmailRecorder(factory);
        recorder.Clear();
        using var publicClient = factory.CreateClient();
        using var staleSessionClient = factory.CreateClient();
        using var freshLoginClient = factory.CreateClient();

        var activeSession = await IntegrationTestSupport.LoginAsync(staleSessionClient, "user@test.local", "User123!Pass");
        IntegrationTestSupport.Authorize(staleSessionClient, activeSession.Token);

        var unknownForgotResponse = await publicClient.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email = "missing@test.local"
        });
        var unknownForgotPayload = await IntegrationTestSupport.ReadEnvelopeAsync<object?>(unknownForgotResponse, HttpStatusCode.OK);

        var knownForgotResponse = await publicClient.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email = "user@test.local"
        });
        var knownForgotPayload = await IntegrationTestSupport.ReadEnvelopeAsync<object?>(knownForgotResponse, HttpStatusCode.OK);

        Assert.Equal("If an account exists for this email, a reset link has been sent.", unknownForgotPayload.Message);
        Assert.Equal(unknownForgotPayload.Message, knownForgotPayload.Message);

        var resetEmail = Assert.Single(
            recorder.Snapshot(),
            message => message.Subject.Contains("Reset your NoorLocator password", StringComparison.Ordinal));
        Assert.Equal("noorlocator@gmail.com", resetEmail.FromEmail);
        Assert.Equal("user@test.local", resetEmail.ToEmail);
        var resetToken = IntegrationTestSupport.ExtractToken(resetEmail);

        var invalidResetResponse = await publicClient.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = "invalid-token",
            password = "Renewed123!",
            confirmPassword = "Renewed123!"
        });
        var invalidResetPayload = await IntegrationTestSupport.ReadEnvelopeAsync<object?>(invalidResetResponse, HttpStatusCode.BadRequest);
        Assert.Equal("This password reset link is invalid.", invalidResetPayload.Message);

        var validResetResponse = await publicClient.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = resetToken,
            password = "Renewed123!",
            confirmPassword = "Renewed123!"
        });
        var validResetPayload = await IntegrationTestSupport.ReadEnvelopeAsync<object?>(validResetResponse, HttpStatusCode.OK);
        Assert.Equal("Your password has been reset successfully.", validResetPayload.Message);

        var staleSessionResponse = await staleSessionClient.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, staleSessionResponse.StatusCode);

        var oldPasswordLoginResponse = await freshLoginClient.PostAsJsonAsync("/api/auth/login", new
        {
            email = "user@test.local",
            password = "User123!Pass"
        });
        var oldPasswordLoginPayload = await IntegrationTestSupport.ReadEnvelopeAsync<AuthPayload>(oldPasswordLoginResponse, HttpStatusCode.Unauthorized);
        Assert.Equal("Invalid email or password.", oldPasswordLoginPayload.Message);

        var newPasswordSession = await IntegrationTestSupport.LoginAsync(freshLoginClient, "user@test.local", "Renewed123!");
        Assert.Equal("user@test.local", newPasswordSession.User!.Email);

        var reusedResetResponse = await publicClient.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = resetToken,
            password = "Another123!",
            confirmPassword = "Another123!"
        });
        var reusedResetPayload = await IntegrationTestSupport.ReadEnvelopeAsync<object?>(reusedResetResponse, HttpStatusCode.BadRequest);
        Assert.Equal("This password reset link is invalid.", reusedResetPayload.Message);

        var confirmationEmail = Assert.Single(
            recorder.Snapshot(),
            message => message.Subject.Contains("password was changed", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("noorlocator@gmail.com", confirmationEmail.FromEmail);
        Assert.Equal("user@test.local", confirmationEmail.ToEmail);

        var secondForgotResponse = await publicClient.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email = "user@test.local"
        });
        await IntegrationTestSupport.ReadEnvelopeAsync<object?>(secondForgotResponse, HttpStatusCode.OK);

        var secondResetEmail = recorder.Snapshot()
            .Where(message => message.Subject.Contains("Reset your NoorLocator password", StringComparison.Ordinal))
            .OrderBy(message => message.CreatedAtUtc)
            .Last();
        var expiredResetToken = IntegrationTestSupport.ExtractToken(secondResetEmail);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<NoorLocatorDbContext>();
            var user = await dbContext.Users.SingleAsync(currentUser => currentUser.Email == "user@test.local");
            user.PasswordResetTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5);
            await dbContext.SaveChangesAsync();
        }

        var expiredResetResponse = await publicClient.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = expiredResetToken,
            password = "Expired123!",
            confirmPassword = "Expired123!"
        });
        var expiredResetPayload = await IntegrationTestSupport.ReadEnvelopeAsync<object?>(expiredResetResponse, HttpStatusCode.Gone);
        Assert.Equal("This password reset link has expired.", expiredResetPayload.Message);
    }
}
