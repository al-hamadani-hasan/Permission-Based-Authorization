using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Permission_Based_Authorization.Claims;
using Permission_Based_Authorization.Constants;
using Permission_Based_Authorization.Helpers.Claims;
using Permission_Based_Authorization.Repositories;

namespace Permission_Based_Authorization.Controllers
{
    [Authorize(Roles = "Administrator")]
    [Route("role")]
    public class RolesController : Controller
    {
        private readonly IRoleRepository _roleRepository;

        public RolesController(
            IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index()
        {
            var roles = await _roleRepository.RolesAsync();

            ViewData["RoleInfo"] = roles;

            return View(roles);
        }

        [HttpGet]
        [Route("permissions")]
        public async Task<IActionResult> Permissions(Guid Id)
        {
            var model = new PermissionClaims();
            var permissions = new List<RoleClaims>();

            permissions.GetPermissions(typeof(Permissions.Predefined));

            var role = await _roleRepository.FindByIdAsync(Id);

            model.RoleId = role.Id.ToString();

            var claims = await _roleRepository.FindClaimsAsync(role);

            var claimValues = permissions.Select(a => a.Value).ToList();
            var roleClaimValues = claims.Select(a => a.Value).ToList();

            var authorizedClaims = claimValues.Intersect(roleClaimValues).ToList();

            foreach (var permission in permissions)
            {
                if (authorizedClaims.Any(a => a == permission.Value))
                {
                    permission.Selected = true;
                }
            }

            model.RoleClaims = permissions;
            return View(model);
        }

        [HttpPost]
        [Route("update-permissions")]
        public async Task<IActionResult> UpdatePermissions(PermissionClaims model)
        {
            var roleId = Guid.Parse(model.RoleId);

            var role = await _roleRepository.FindByIdAsync(roleId);

            await _roleRepository.RemoveClaimAsync(role);

            var selectedClaims = model.RoleClaims.Where(a => a.Selected).ToList();
            foreach (var claim in selectedClaims)
            {
                await _roleRepository.CreateClaimAsync(role, claim.Value);
            }

            return RedirectToActionPermanent("Index");
        }
    }
}
