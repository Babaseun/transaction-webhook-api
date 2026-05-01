using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Transactions.Data;
using Transactions.Domain.Models;
using Xunit;

namespace Transactions.Test;

public class TransactionWebhookTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly Mock<ITransactionRepository> _mockRepo;

    public TransactionWebhookTests(WebApplicationFactory<Program> factory)
    {
        _mockRepo = new Mock<ITransactionRepository>();

        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Swap the real repository for the mocked one
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITransactionRepository));
                if (descriptor != null) services.Remove(descriptor);
                
                services.AddScoped(_ => _mockRepo.Object);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task PostWebhook_ValidPayload_ReturnsCreatedAndCalculatesFee()
    {
        // Arrange
        _mockRepo.Setup(r => r.ExistsByExternalIdAsync(It.IsAny<string>())).ReturnsAsync(false);
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
        _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var payload = new { TransactionId = "txn_123", Amount = 100.00m, Currency = "USD", Merchant = "Acme Corp", Timestamp = DateTime.UtcNow };
        
        // Act
        var response = await _client.PostAsJsonAsync("/webhooks/transactions", payload);
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1.5m, result.GetProperty("calculatedFee").GetDecimal());
        Assert.Equal(98.5m, result.GetProperty("netAmount").GetDecimal());

        // Verify Repository behavior
        _mockRepo.Verify(r => r.ExistsByExternalIdAsync("txn_123"), Times.Once);
        _mockRepo.Verify(r => r.AddAsync(It.Is<Transaction>(t => t.ExternalTransactionId == "txn_123" && t.Amount == 100.00m)), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task PostWebhook_DuplicatePayload_ReturnsOkIdempotent()
    {
        // Arrange
        _mockRepo.Setup(r => r.ExistsByExternalIdAsync("txn_124")).ReturnsAsync(true);

        var payload = new { TransactionId = "txn_124", Amount = 50.00m, Currency = "USD", Merchant = "Acme Corp", Timestamp = DateTime.UtcNow };
        
        // Act
        var duplicateResponse = await _client.PostAsJsonAsync("/webhooks/transactions", payload);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, duplicateResponse.StatusCode); // Returns 200 OK without recreating
        
        // Verify Repository behavior
        _mockRepo.Verify(r => r.ExistsByExternalIdAsync("txn_124"), Times.Once);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
        _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}
