using Microsoft.AspNetCore.Mvc;
using LMS.Core.Interfaces.Services;
using LMS.Application.DTOs;
using LMS.Core.Models;

namespace LMS.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;

        public AccountController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var isValid = await _userService.ValidateUserCredentialsAsync(model.Email, model.Password);
            if (!isValid)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            var user = await _userService.GetUserByEmailAsync(model.Email);
            if (user != null)
            {
                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");
                HttpContext.Session.SetString("UserRole", user.Role.ToString());

                TempData["Success"] = "Login successful!";
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "Logged out successfully";
            return RedirectToAction("Index", "Home");
        }
    }
}