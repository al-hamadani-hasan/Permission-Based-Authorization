using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Permission_Based_Authorization.Repositories;
using Permission_Based_Authorization.Models;

namespace Permission_Based_Authorization.Controllers
{
    [Route("account")]
    public class AccountController : Controller
    {
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly IUserRepository _userRepository;

        public AccountController(
            IAuthenticationRepository authenticationRepository, 
            IUserRepository userRepository)
        {
            _authenticationRepository = authenticationRepository;
            _userRepository = userRepository;
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("login")]
        public IActionResult Login(
            String redirect = "~/")
        {
            // Check if the user is already authenticated
            if (User.Identity!.IsAuthenticated)
            {
                return Redirect(string.IsNullOrEmpty(redirect) ? "~/" : redirect);
            }

            ViewData["redirect"] = redirect;
            ModelState.Clear();
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [Route("login")]
        public async Task<IActionResult> LoginAsync(
            LoginViewModel model,
            String redirect = "~/")
        {
            redirect ??= Request.Form["redirect"]!;
            ViewData["redirect"] = redirect;

            var user = await _userRepository.ValidationAsync(
                username: model.Username!.Trim(),
                password: model.Password!.Trim());

            if (user.Id == Guid.Empty &&
                user!.LockoutEnabled is false)
            {
                ModelState.AddModelError("ValidationSummary", "أسم المستخدم او كلمة السر غير صحيحة.");
                return View(model);
            }
            else if (user.LockoutEnabled)
            {
                ModelState.AddModelError("ValidationSummary", "لقد تم قفل حسابك مؤقتًا لأسباب أمنية. للحصول على المساعدة، يرجى التواصل مع فريق الدعم لدينا لمساعدتك على استعادة الوصول.");
                return View(model);
            }

            await _authenticationRepository.SignInAsync(
                httpContext: HttpContext,
                user: user,
                isPersistent: false);

            if (!string.IsNullOrEmpty(redirect)
                && Url.IsLocalUrl(redirect))
                return Redirect(redirect);
            else
                return RedirectPermanent("~/");
        }

        [Route("logout")]
        public async Task<IActionResult> LogoutAsync()
        {
            await _authenticationRepository.SignOutAsync(
                httpContext: this.HttpContext);

            return RedirectPermanent("~/");
        }

        [HttpGet]
        [Route("access-denied")]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
