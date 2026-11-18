using AutoFixture;
using Simulation.Checkout.Domain;

namespace Simulation.Checkout.Tests.TestData;

public static class OrderFixture
{
    public static Order CreateValidOrder(int itemCount = 3)
    {
        var fixture = new Fixture();
        var random = new Random(42);

        fixture.Customize<OrderItem>(c => c
            .With(i => i.Quantity, () => random.Next(1, 10))
            .With(i => i.UnitPrice, () => Math.Round((decimal)random.NextDouble() * 500m, 2)));

        fixture.Customize<Order>(c => c
            .With(o => o.Items, () => (IReadOnlyList<OrderItem>)fixture.CreateMany<OrderItem>(itemCount).ToList()));

        return fixture.Create<Order>();
    }
}