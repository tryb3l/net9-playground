using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using WebApplication1.ViewModels;

namespace WebApplication1.Interfaces;

public interface IAccountService
{
    Task<SignInResult> SignInUserAsync(string email, string password, bool rememberMe);
    Task<IdentityResult> RegisterUserAsync(RegisterViewModel model);
    Task SignOutAsync();
    Task<ExternalLoginInfo> GetExternalLoginInfoAsync();
    Task<SignInResult> ExternalLoginSignInAsync(string loginProvider, string providerKey, bool isPersistent);
    Task<IdentityResult> CreateUserWithExternalLoginAsync(ExternalLoginInfo info);
    AuthenticationProperties ConfigureExternalAuthenticationProperties(string provider, string redirectUrl);
}