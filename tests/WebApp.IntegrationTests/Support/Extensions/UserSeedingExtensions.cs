using WebApp.IntegrationTests.Data;
using WebApp.Models;

namespace WebApp.IntegrationTests.Support.Extensions;

public static class UserSeedingExtensions
{
    public static async Task SeedUserAsync(this BaseIntegrationTest test, TestUser user)
    {
        await test.ExecuteDbContextAsync(async db =>
        {
            if (await db.Users.FindAsync(user.Id) != null) return;

            var dbUser = new User
            {
                Id = user.Id,
                UserName = user.Email, 
                Email = user.Email,
                EmailConfirmed = true,
                NormalizedEmail = user.Email.ToUpper(),
                NormalizedUserName = user.Email.ToUpper(),
                SecurityStamp = Guid.NewGuid().ToString(),
            };
            
            db.Users.Add(dbUser);
            await db.SaveChangesAsync();
        });
    }
}