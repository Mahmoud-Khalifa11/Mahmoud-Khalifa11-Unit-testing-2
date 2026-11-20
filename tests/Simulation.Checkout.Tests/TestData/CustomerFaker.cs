using Bogus;

namespace Simulation.Checkout.Tests.TestData;

public static class CustomerFaker
{
    private const int Seed = 1234;

    public static (string Name, string Email) CreateCustomer()
    {
        var faker = new Faker("en") { Random = new Randomizer(Seed) };

        var firstName = faker.Name.FirstName();
        var lastName = faker.Name.LastName();
        var email = faker.Internet.Email(firstName, lastName);

        return ($"{firstName} {lastName}", email);
    }
}