namespace WebApp.IntegrationTests.Data;

public sealed record TestUser(
    string Id, 
    string Email, 
    string Name, 
    string[] Roles
    );