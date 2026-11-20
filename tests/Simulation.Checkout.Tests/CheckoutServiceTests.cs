using FluentAssertions;
using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using Simulation.Checkout.Domain.Exceptions;
using Xunit;
using Simulation.Checkout.Tests.TestData;

namespace Simulation.Checkout.Tests;

public class CheckoutServiceTests
{
    private readonly Mock<IInventoryService> _inventory = new();
    private readonly Mock<IOrderRepository> _repository = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly CheckoutService _sut;

    public CheckoutServiceTests()
    {
        _inventory
            .Setup(i => i.IsAvailableAsync(
                It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _sut = new CheckoutService(
            _repository.Object,
            _inventory.Object,
            _notifications.Object);
    }

    [Fact]
    public async Task CheckoutAsync_WhenOrderIsEmpty_ShouldThrowEmptyOrderException()
    {
        var order = CreateOrder();

        var act = () => _sut.CheckoutAsync(order);

        await act.Should().ThrowAsync<EmptyOrderException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CheckoutAsync_WhenQuantityIsNotPositive_ShouldThrowInvalidQuantityExceptionWithProductName(int quantity)
    {
        var order = CreateOrder(CreateItem(name: "Laptop", quantity: quantity));

        var act = () => _sut.CheckoutAsync(order);

        await act.Should().ThrowAsync<InvalidQuantityException>()
            .WithMessage("*Laptop*");
    }

    [Fact]
    public async Task CheckoutAsync_WhenUnitPriceIsNegative_ShouldThrowInvalidPriceExceptionWithProductName()
    {
        var order = CreateOrder(CreateItem(name: "Mouse", price: -0.01m));

        var act = () => _sut.CheckoutAsync(order);

        await act.Should().ThrowAsync<InvalidPriceException>()
            .WithMessage("*Mouse*");
    }


    [Fact]
    public async Task CheckoutAsync_WhenUnitPriceIsZero_ShouldSucceedWithZeroTotal()
    {
        var order = CreateOrder(CreateItem(price: 0m, quantity: 2));

        var receipt = await _sut.CheckoutAsync(order);

        receipt.Subtotal.Should().Be(0m);
        receipt.Total.Should().Be(0m);
    }


    public static TheoryData<decimal, decimal> DiscountCases => new()
    {
        { 999.99m, 0m },
        { 1000m, 100m },
        { 1500m, 100m }
    };

    [Theory]
    [MemberData(nameof(DiscountCases))]
    public async Task CheckoutAsync_WhenSubtotalIsAroundThreshold_ShouldApplyExpectedDiscount(
        decimal subtotal, decimal expectedDiscount)
    {
        var order = CreateOrder(CreateItem(price: subtotal, quantity: 1));

        var receipt = await _sut.CheckoutAsync(order);

        receipt.Discount.Should().Be(expectedDiscount);
    }


    public static TheoryData<OrderItem[], decimal, decimal, decimal> ReceiptAmountCases => new()
    {
        // Example A
        {
            new[]
            {
                CreateItem(name: "Item 1", price: 150m, quantity: 2),
                CreateItem(name: "Item 2", price: 400m, quantity: 1)
            },
            700m, 0m, 700m
        },
        // Example C
        {
            new[] { CreateItem(name: "Item 1", price: 500m, quantity: 3) },
            1500m, 100m, 1400m
        }
    };

    [Theory]
    [MemberData(nameof(ReceiptAmountCases))]
    public async Task CheckoutAsync_WithValidItems_ShouldCalculateSubtotalDiscountAndTotal(
        OrderItem[] items, decimal expectedSubtotal, decimal expectedDiscount, decimal expectedTotal)
    {
        var order = CreateOrder(items);

        var receipt = await _sut.CheckoutAsync(order);

        receipt.Subtotal.Should().Be(expectedSubtotal);
        receipt.Discount.Should().Be(expectedDiscount);
        receipt.Total.Should().Be(expectedTotal);
    }


    [Fact]
    public async Task CheckoutAsync_WhenAllItemsInStock_ShouldSaveOrderAndSendConfirmationOnce()
    {
        var order = CreateOrder(CreateItem(price: 200m, quantity: 2));

        var receipt = await _sut.CheckoutAsync(order);

        receipt.OrderId.Should().Be(order.Id);
        _repository.Verify(
            r => r.SaveAsync(order, It.IsAny<CancellationToken>()),
            Times.Once);
        _notifications.Verify(
            n => n.SendOrderConfirmationAsync(receipt, It.IsAny<CancellationToken>()),
            Times.Once);
    }



    [Fact]
    public async Task CheckoutAsync_WhenOneItemIsOutOfStock_ShouldThrowAndNotSaveOrSendConfirmation()
    {
        var inStockItem = CreateItem(name: "Keyboard");
        var outOfStockItem = CreateItem(name: "Monitor");
        var order = CreateOrder(inStockItem, outOfStockItem);

        _inventory
            .Setup(i => i.IsAvailableAsync(
                outOfStockItem.ProductId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _sut.CheckoutAsync(order);

        await act.Should().ThrowAsync<InsufficientInventoryException>()
            .WithMessage("*Monitor*");
        _repository.Verify(
            r => r.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notifications.Verify(
            n => n.SendOrderConfirmationAsync(It.IsAny<CheckoutReceipt>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }



    [Fact]
    public async Task CheckoutAsync_WhenValidationFails_ShouldNotCheckInventoryOrSaveOrSendConfirmation()
    {
        var order = CreateOrder(CreateItem(quantity: 0));

        var act = () => _sut.CheckoutAsync(order);

        await act.Should().ThrowAsync<InvalidQuantityException>();
        _inventory.Verify(
            i => i.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repository.Verify(
            r => r.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notifications.Verify(
            n => n.SendOrderConfirmationAsync(It.IsAny<CheckoutReceipt>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }


    [Fact]
    public async Task CheckoutAsync_WhenOrderIsCreatedByAutoFixture_ShouldSucceed()
    {
        var order = OrderFixture.CreateValidOrder();

        var receipt = await _sut.CheckoutAsync(order);

        receipt.OrderId.Should().Be(order.Id);
        receipt.Items.Should().HaveCount(order.Items.Count);
        receipt.Total.Should().BeGreaterThanOrEqualTo(0m);
    }



    [Fact]
    public async Task CheckoutAsync_WhenCustomerIsCreatedByBogus_ShouldCopyNameAndEmailToReceipt()
    {
        var (name, email) = CustomerFaker.CreateCustomer();
        var order = new Order
        {
            CustomerName = name,
            CustomerEmail = email,
            Items = new[] { CreateItem() }
        };

        var receipt = await _sut.CheckoutAsync(order);

        receipt.CustomerName.Should().Be(name);
        receipt.CustomerEmail.Should().Be(email);
    }


    private static Order CreateOrder(params OrderItem[] items) => new()
    {
        CustomerName = "Test Customer",
        CustomerEmail = "test@example.com",
        Items = items
    };

    private static OrderItem CreateItem(string name = "Product", decimal price = 100m, int quantity = 1) => new()
    {
        ProductId = Guid.NewGuid(),
        ProductName = name,
        UnitPrice = price,
        Quantity = quantity
    };
}

