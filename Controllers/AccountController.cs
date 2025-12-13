using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PeP.Models;

namespace PeP.Controllers
{
    [Route("Account")]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpPost("ProcessLogin")]
        public async Task<IActionResult> ProcessLogin(string email, string password, bool rememberMe = false)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return Redirect("/Account/Login?error=invalid");
                }

                var result = await _signInManager.PasswordSignInAsync(user.UserName, password, rememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    return Redirect("/");
                }
                else if (result.IsLockedOut)
                {
                    return Redirect("/Account/Login?error=locked");
                }
                else if (result.RequiresTwoFactor)
                {
                    return Redirect("/Account/Login?error=2fa");
                }
                else
                {
                    return Redirect("/Account/Login?error=invalid");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                return Redirect("/Account/Login?error=exception");
            }
        }

        [HttpPost("ProcessLogout")]
        public async Task<IActionResult> ProcessLogout()
        {
            try
            {
                await _signInManager.SignOutAsync();
                return Redirect("/");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logout error: {ex.Message}");
                return Redirect("/");
            }
        }
    }
}