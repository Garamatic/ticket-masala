using IT_Project2526.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT_Project2526.Controllers
{
	//TODO: authorization
	/// <summary>
	/// Controller that manages CRUD operations for <see cref="ApplicationUser"/> entities.
	/// Exposes actions to list, view details, create, edit and delete application users.
	/// </summary>
	public class ApplicationUsersController : Controller
	{
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly ILogger<ApplicationUsersController> _logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="ApplicationUsersController"/> class.
		/// </summary>
		/// <param name="userManager">The user manager used to perform identity operations.</param>
		/// <param name="logger">The logger instance for diagnostic logging.</param>
		public ApplicationUsersController(UserManager<ApplicationUser> userManager, ILogger<ApplicationUsersController> logger)
		{
			_userManager = userManager;
			_logger = logger;
		}

		/// <summary>
		/// GET: /ApplicationUsers
		/// Returns a view containing all application users.
		/// </summary>
		/// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="IActionResult"/> that renders the users list view.</returns>
		public async Task<IActionResult> Index()
		{
			try
			{
				var users = await _userManager.Users.AsNoTracking().ToListAsync();
				return View(users);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to load users for Index");
				return Problem("An unexpected error occurred while loading users.");
			}
		}

		/// <summary>
		/// GET: /ApplicationUsers/Details/{id}
		/// Shows details for the user identified by <paramref name="id" />.
		/// </summary>
		/// <param name="id">The identifier of the user to display.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="IActionResult"/> that renders the user details view or an error result.</returns>
		public async Task<IActionResult> Details(string id)
		{
			if (string.IsNullOrEmpty(id)) return BadRequest();

			try
			{
				var user = await _userManager.FindByIdAsync(id);
				if (user == null) return NotFound();
				return View(user);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to load user details for Id: {Id}", id);
				return Problem("An unexpected error occurred while loading user details.");
			}
		}

		/// <summary>
		/// GET: /ApplicationUsers/Create
		/// Returns the create user view.
		/// </summary>
		/// <returns>An <see cref="IActionResult"/> that renders the create view.</returns>
		public IActionResult Create()
		{
			return View();
		}

		/// <summary>
		/// POST: /ApplicationUsers/Create
		/// Creates a new application user using the provided model and password.
		/// </summary>
		/// <param name="model">The user model to create.</param>
		/// <param name="password">The password for the new user account.</param>
		/// <returns>A task that represents the asynchronous operation. Redirects to the index on success or re-renders the create view on failure.</returns>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(ApplicationUser model, string password)
		{
			if (model == null) return BadRequest();

			if (string.IsNullOrEmpty(password))
				ModelState.AddModelError(nameof(password), "Password is required.");

			if (!ModelState.IsValid)
				return View(model);

			try
			{
				var result = await _userManager.CreateAsync(model, password);

				if (result.Succeeded)
					return RedirectToAction(nameof(Index));

				foreach (var error in result.Errors)
					ModelState.AddModelError("", error.Description);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to create user {UserName}", model?.UserName);
				ModelState.AddModelError("", "An unexpected error occurred while creating the user.");
			}

			return View(model);
		}

		/// <summary>
		/// GET: /ApplicationUsers/Edit/{id}
		/// Loads the edit view for the user identified by <paramref name="id" />.
		/// </summary>
		/// <param name="id">The identifier of the user to edit.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="IActionResult"/> that renders the edit view or an error result.</returns>
		public async Task<IActionResult> Edit(string id)
		{
			if (string.IsNullOrEmpty(id)) return BadRequest();

			try
			{
				var user = await _userManager.FindByIdAsync(id);
				if (user == null) return NotFound();
				return View(user);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to load user for edit Id: {Id}", id);
				return Problem("An unexpected error occurred while loading user for edit.");
			}
		}

		/// <summary>
		/// POST: /ApplicationUsers/Edit/{id}
		/// Persists changes to an existing user.
		/// </summary>
		/// <param name="model">The updated user model. The <see cref="ApplicationUser.Id"/> property must be populated.</param>
		/// <returns>A task that represents the asynchronous operation. Redirects to the index on success or re-renders the edit view on failure.</returns>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(ApplicationUser model)
		{
			if (model == null || string.IsNullOrEmpty(model.Id)) return BadRequest();

			if (!ModelState.IsValid)
				return View(model);

			try
			{
				var user = await _userManager.FindByIdAsync(model.Id);
				if (user == null) return NotFound();

				user.FirstName = model.FirstName;
				user.LastName = model.LastName;
				user.Phone = model.Phone;
				user.Email = model.Email;
				user.UserName = model.UserName;

				var result = await _userManager.UpdateAsync(user);
				if (result.Succeeded)
					return RedirectToAction(nameof(Index));

				foreach (var error in result.Errors)
					ModelState.AddModelError("", error.Description);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to update user Id: {Id}", model.Id);
				ModelState.AddModelError("", "An unexpected error occurred while updating the user.");
			}

			return View(model);
		}

		/// <summary>
		/// GET: /ApplicationUsers/Delete/{id}
		/// Shows a confirmation view to delete the specified user.
		/// </summary>
		/// <param name="id">The identifier of the user to delete.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="IActionResult"/> that renders the delete confirmation view or an error result.</returns>
		public async Task<IActionResult> Delete(string id)
		{
			if (string.IsNullOrEmpty(id)) return BadRequest();

			try
			{
				var user = await _userManager.FindByIdAsync(id);
				if (user == null) return NotFound();

				return View(user);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to load user for delete Id: {Id}", id);
				return Problem("An unexpected error occurred while loading user for delete.");
			}
		}

		/// <summary>
		/// POST: /ApplicationUsers/Delete/{id}
		/// Deletes the user identified by <paramref name="id" />.
		/// </summary>
		/// <param name="id">The identifier of the user to delete.</param>
		/// <returns>A task that represents the asynchronous operation. Redirects to the index on success or returns to the delete view on failure.</returns>
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(string id)
		{
			if (string.IsNullOrEmpty(id)) return BadRequest();

			try
			{
				var user = await _userManager.FindByIdAsync(id);
				if (user != null)
				{
					var result = await _userManager.DeleteAsync(user);
					if (!result.Succeeded)
					{
						foreach (var error in result.Errors)
							_logger.LogWarning("Failed to delete user Id: {Id} Error: {Error}", id, error.Description);

						ModelState.AddModelError("", "Failed to delete user.");
						return RedirectToAction(nameof(Delete), new { id });
					}
				}

				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to delete user Id: {Id}", id);
				return Problem("An unexpected error occurred while deleting the user.");
			}
		}
	}
}
