using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Permission_Based_Authorization.Models;
using Permission_Based_Authorization.Repositories;
using System.Xml;
using X.PagedList.Extensions;

namespace Permission_Based_Authorization.Controllers
{
    [Route("user")]
    [Authorize(Roles = "Administrator")]
    public class UsersController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;

        public UsersController(
            IConfiguration configuration,
            IUserRepository userRepository)
        {
            _configuration = configuration;
            _userRepository = userRepository;
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index(int? page = 1)
        {
            int pageSize = _configuration.GetValue<int>("PaginationSettings:PageSize");

            var user = await _userRepository.CurrentUserAsync(HttpContext);

            var usersExceptResult = await _userRepository.ExceptUserByIdAsync(user.Id);
            var users = usersExceptResult.ToPagedList(page ?? 1, pageSize);

            return View(users);
        }

    }
}
