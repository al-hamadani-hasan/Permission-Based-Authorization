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

        [HttpGet]
        [Route("create")]
        public IActionResult CreateUser()
        {
            var model = new AddUserViewModel();
            return View(model);
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateUser(AddUserViewModel userViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(userViewModel);
            }

            var isNotAvailable = await _userRepository.FindByUsernameAsync(userViewModel.Username!);

            if (isNotAvailable)
            {
                userViewModel.Username = string.Empty;

                return View(userViewModel);
            }

            await _userRepository.CreateAsync(userViewModel);

            return RedirectToAction("Index");
        }

        [HttpGet]
        [Route("change-status")]
        public async Task<IActionResult> ChangeStatus(Guid Id)
        {
            var user = await _userRepository.FindByIdAsync(Id);

            if (user != null)
            {
                await _userRepository.ChangeStatusByIdAsync(Id);
            }
            
            return RedirectToAction("Index");
        }
    }
}
