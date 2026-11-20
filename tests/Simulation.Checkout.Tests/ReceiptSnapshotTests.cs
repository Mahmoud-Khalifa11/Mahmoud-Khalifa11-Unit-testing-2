using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using VerifyXunit;
using Xunit;

namespace Simulation.Checkout.Tests;

public class ReceiptSnapshotTests
{
    [Fact]
    public async Task CheckoutAsync_ForExampleC_ShouldMatchReceiptSnapshot()
    {
        var inventory = new Mock<IInventoryService>();
        inventory
            .Setup(i => i.IsAvailableAsync(
                It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new CheckoutService(
            new Mock<IOrderRepository>().Object,
            inventory.Object,
            new Mock<INotificationService>().Object);

        var order = new Order
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CustomerName = "Test Customer",
            CustomerEmail = "test@example.com",
            Items = new[]
            {
                new OrderItem
                {
                    ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    ProductName = "Item 1",
                    UnitPrice = 500m,
                    Quantity = 3
                }
            }
        };

        var receipt = await sut.CheckoutAsync(order);

        await Verifier.Verify(receipt);
    }
}