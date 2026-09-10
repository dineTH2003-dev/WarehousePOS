using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WarehousePOS.Application.Notifications;
using WarehousePOS.Application.Reports;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Interfaces;
using WarehousePOS.Infrastructure.Notifications;
using Xunit;

namespace WarehousePOS.Infrastructure.Tests;

public sealed class NotificationServiceTests
{
    [Fact]
    public async Task BrevoEmailService_WithoutApiKey_ShouldReturnError()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var service = new BrevoEmailService(httpClient, NullLogger<BrevoEmailService>.Instance);

        // Act
        var result = await service.SendTestEmailAsync("owner@example.com");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Brevo API key is not configured");
    }

    [Fact]
    public async Task BrevoEmailService_WithoutRecipient_ShouldReturnError()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var service = new BrevoEmailService(httpClient, NullLogger<BrevoEmailService>.Instance);
        service.Configure("dummy-key", "sender@example.com", "WarehousePOS");

        // Act
        var result = await service.SendTestEmailAsync("");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Recipient email address is required");
    }

    [Fact]
    public async Task WhatsAppNotificationService_WithoutGatewayUrl_ShouldReturnError()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var service = new WhatsAppNotificationService(httpClient, NullLogger<WhatsAppNotificationService>.Instance);

        // Act
        var result = await service.SendTextMessageAsync("+94711435343", "Hello World");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("WhatsApp Gateway URL is not configured");
    }

    [Fact]
    public async Task WhatsAppNotificationService_EmptyItems_ShouldReturnSuccess()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var service = new WhatsAppNotificationService(httpClient, NullLogger<WhatsAppNotificationService>.Instance);

        // Act
        var result = await service.SendLowStockAlertAsync([], "+94711435343");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("No low stock items");
    }

    [Fact]
    public async Task NotificationOrchestrator_SaveAndGetSettings_ShouldPersistSettings()
    {
        // Arrange
        var settingsDict = new Dictionary<string, string>();
        var settingRepoMock = new Mock<IStoreSettingRepository>();
        settingRepoMock.Setup(r => r.GetValueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, CancellationToken _) => settingsDict.TryGetValue(key, out var v) ? v : null);
        settingRepoMock.Setup(r => r.SetValueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string?, CancellationToken>((k, v, _, _) => settingsDict[k] = v)
            .Returns(Task.CompletedTask);

        var productRepoMock = new Mock<IProductRepository>();
        var reportServiceMock = new Mock<IReportService>();
        using var httpClient = new HttpClient();
        var emailService = new BrevoEmailService(httpClient, NullLogger<BrevoEmailService>.Instance);
        var waService = new WhatsAppNotificationService(httpClient, NullLogger<WhatsAppNotificationService>.Instance);

        var orchestrator = new NotificationOrchestrator(
            settingRepoMock.Object,
            productRepoMock.Object,
            reportServiceMock.Object,
            emailService,
            waService,
            NullLogger<NotificationOrchestrator>.Instance);

        var input = new NotificationSettingsDto(
            BrevoApiKey: "xkeysib-test-12345",
            BrevoSenderEmail: "alerts@mystore.com",
            BrevoSenderName: "My Store Alerts",
            OwnerEmail: "owner@mystore.com",
            IsEmailLowStockAlertEnabled: true,
            IsEmailMonthlyReportEnabled: true,
            IsWhatsAppEnabled: true,
            OwnerPhone: "+94711435343",
            WhatsAppGatewayUrl: "https://api.ultramsg.com/instance123/messages/chat",
            WhatsAppApiKey: "token-abc-xyz");

        // Act
        await orchestrator.SaveSettingsAsync(input);
        var loaded = await orchestrator.GetSettingsAsync();

        // Assert
        loaded.BrevoApiKey.Should().Be("xkeysib-test-12345");
        loaded.BrevoSenderEmail.Should().Be("alerts@mystore.com");
        loaded.BrevoSenderName.Should().Be("My Store Alerts");
        loaded.OwnerEmail.Should().Be("owner@mystore.com");
        loaded.IsEmailLowStockAlertEnabled.Should().BeTrue();
        loaded.IsEmailMonthlyReportEnabled.Should().BeTrue();
        loaded.IsWhatsAppEnabled.Should().BeTrue();
        loaded.OwnerPhone.Should().Be("+94711435343");
        loaded.WhatsAppGatewayUrl.Should().Be("https://api.ultramsg.com/instance123/messages/chat");
        loaded.WhatsAppApiKey.Should().Be("token-abc-xyz");
    }

    [Fact]
    public async Task NotificationOrchestrator_CheckAndSendLowStockAlerts_NoLowStock_ShouldReturnSuccess()
    {
        // Arrange
        var settingRepoMock = new Mock<IStoreSettingRepository>();
        var productRepoMock = new Mock<IProductRepository>();
        productRepoMock.Setup(p => p.GetLowStockAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var reportServiceMock = new Mock<IReportService>();
        using var httpClient = new HttpClient();
        var emailService = new BrevoEmailService(httpClient, NullLogger<BrevoEmailService>.Instance);
        var waService = new WhatsAppNotificationService(httpClient, NullLogger<WhatsAppNotificationService>.Instance);

        var orchestrator = new NotificationOrchestrator(
            settingRepoMock.Object,
            productRepoMock.Object,
            reportServiceMock.Object,
            emailService,
            waService,
            NullLogger<NotificationOrchestrator>.Instance);

        // Act
        var result = await orchestrator.CheckAndSendLowStockAlertsAsync(force: true);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("All products have healthy stock levels");
    }
}
