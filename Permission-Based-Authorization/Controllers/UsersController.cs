using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Permission_Based_Authorization.Contexts.UserManagement.Models;
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
        private readonly IRoleRepository _roleRepository;

        public UsersController(
            IConfiguration configuration,
            IUserRepository userRepository,
            IRoleRepository roleRepository)
        {
            _configuration = configuration;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
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

        [HttpGet]
        [Route("roles")]
        public async Task<IActionResult> UserRoles(Guid Id)
        {
            var userRoles = new List<UserRolesViewModel>();
            var user = await _userRepository.FindByIdAsync(Id);

            var roles = await _roleRepository.RolesAsync();

            foreach (var role in roles)
            {
                var userRolesViewModel = new UserRolesViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name!
                };

                var checkResult = await _roleRepository.IsInRoleAsync(user, role.Id);

                if (checkResult)
                {
                    userRolesViewModel.Selected = true;
                }
                else
                {
                    userRolesViewModel.Selected = false;
                }

                userRoles.Add(userRolesViewModel);
            }

            var model = new ManageUserRolesViewModel()
            {
                UserId = Id,
                UserRoles = userRoles
            };

            return View(model);
        }

        [HttpPost]
        [Route("roles")]
        public async Task<IActionResult> UserRoles(Guid Id, ManageUserRolesViewModel model)
        {
            var user = await _userRepository.FindByIdAsync(Id);

            var currentRoles = await _userRepository.RolesAsync(user);

            var selectedRoles = model.UserRoles
                .Where(x => x.Selected)
                .ToList();

            var newRoles = new List<Role>();

            foreach (var role in selectedRoles)
            {
                newRoles.Add(new Role
                {
                    Id = role.RoleId,
                    Name = role.RoleName
                });
            }

            var deleteRoles = await _userRepository.RemoveFromRolesAsync(user, currentRoles);

            await _userRepository.AddToRolesAsync(user, newRoles);
            
            return RedirectToActionPermanent("Index");
        }
    }
}
