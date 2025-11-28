using IT_Project2526.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT_Project2526.Managers
{
    public class ApplicationUserManager
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        //constructor - initialize
        public ApplicationUserManager(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        //Get all users
        public async Task<List<ApplicationUser>> GetAllUsersAsync()
        {
            return await _userManager.Users.ToListAsync();
        }

        //Get user by ID
        public async Task<ApplicationUser?> GetUserByIdAsync(string id)
        {
            return await _userManager.FindByIdAsync(id);
        }

        //Create a user with a certain role
        public async Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password, string? role = null)
        {
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded && !string.IsNullOrEmpty(role))
            {
                await EnsureRoleExistsAsync(role);
                await _userManager.AddToRoleAsync(user, role);
            }
            return result;
        }

        //Update a user
        public async Task<IdentityResult> UpdateUserAsync(ApplicationUser user)
        {
            return await _userManager.UpdateAsync(user);
        }

        //Delete a user
        public async Task<IdentityResult> DeleteUserAsync(ApplicationUser user)
        {
            return await _userManager.DeleteAsync(user);
        }

        //Get all roles
        public async Task<List<string>> GetAllRolesAsync()
        {
            return await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        }

        //Create role if it doesn’t exist
        public async Task EnsureRoleExistsAsync(string role)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
        }

        //Add user to role
        public async Task<IdentityResult> AddUserToRoleAsync(ApplicationUser user, string role)
        {
            await EnsureRoleExistsAsync(role);
            return await _userManager.AddToRoleAsync(user, role);
        }

        //Remove user from role
        public async Task<IdentityResult> RemoveUserFromRoleAsync(ApplicationUser user, string role)
        {
            return await _userManager.RemoveFromRoleAsync(user, role);
        }

        //Check if user is in role
        public async Task<bool> IsUserInRoleAsync(ApplicationUser user, string role)
        {
            return await _userManager.IsInRoleAsync(user, role);
        }

        //Get roles for user
        public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }

        //Get all users by type
        public async Task<List<T>> GetUsersByTypeAsync<T>() where T : ApplicationUser
        {
            // Because T derives from ApplicationUser, we can safely cast.
            return await _userManager.Users.OfType<T>().ToListAsync();
        }

        //helpers for convenience
        public async Task<List<Employee>> GetEmployeesAsync()
        {
            return await _userManager.Users.OfType<Employee>().ToListAsync();
        }

        public async Task<List<Customer>> GetCustomersAsync()
        {
            return await _userManager.Users.OfType<Customer>().ToListAsync();
        }
    }
}
