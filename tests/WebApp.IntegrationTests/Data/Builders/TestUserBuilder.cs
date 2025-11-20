namespace WebApp.IntegrationTests.Data.Builders;

public sealed class TestUserBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private string _email = $"user-{Guid.NewGuid()}@example.com";
    private const string Name = "Test User";
    private readonly HashSet<string> _roles = [];

    public TestUserBuilder WithId(string id) { _id = id; return this; }
    public TestUserBuilder WithEmail(string email) { _email = email; return this; }
    public TestUserBuilder WithRole(string role) { _roles.Add(role); return this; }
    
    public TestUserBuilder AsAdmin() => WithRole("Admin");
    public TestUserBuilder AsUser() => WithRole("User");

    public TestUser Build() => new(_id, _email, Name, _roles.ToArray());
}