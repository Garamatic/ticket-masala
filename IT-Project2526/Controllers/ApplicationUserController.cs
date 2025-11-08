using IT_Project2526.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IT_Project2526.Controllers
{
	//TODO: authorization
	public class ApplicationUsersController : Controller
	{
		private readonly UserManager<ApplicationUser> _userManager;

		public ApplicationUsersController(UserManager<ApplicationUser> userManager)
		{
			_userManager = userManager;
		}

		// GET: /ApplicationUsers
		public IActionResult Index()
		{
			var users = _userManager.Users.ToList();
			return View(users);
		}

		// GET: /ApplicationUsers/Details/{id}
		public async Task<IActionResult> Details(string id)
		{
			var user = await _userManager.FindByIdAsync(id);
			if (user == null) return NotFound();
			return View(user);
		}

		// GET: /ApplicationUsers/Create
		public IActionResult Create()
		{
			return View();
		}

		// POST: /ApplicationUsers/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(ApplicationUser model, string password)
		{
			if (ModelState.IsValid)
			{
				var result = await _userManager.CreateAsync(model, password);

				if (result.Succeeded)
					return RedirectToAction(nameof(Index));

				foreach (var error in result.Errors)
					ModelState.AddModelError("", error.Description);
			}

			return View(model);
		}

		// GET: /ApplicationUsers/Edit/{id}
		public async Task<IActionResult> Edit(string id)
		{
			var user = await _userManager.FindByIdAsync(id);
			if (user == null) return NotFound();
			return View(user);
		}

		// POST: /ApplicationUsers/Edit/{id}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(ApplicationUser model)
		{
			var user = await _userManager.FindByIdAsync(model.Id);
			if (user == null) return NotFound();

			if (ModelState.IsValid)
			{
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

			return View(model);
		}

		// GET: /ApplicationUsers/Delete/{id}
		public async Task<IActionResult> Delete(string id)
		{
			var user = await _userManager.FindByIdAsync(id);
			if (user == null) return NotFound();

			return View(user);
		}

		// POST: /ApplicationUsers/Delete/{id}
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(string id)
		{
			var user = await _userManager.FindByIdAsync(id);
			if (user != null)
				await _userManager.DeleteAsync(user);

			return RedirectToAction(nameof(Index));
		}
	}
}
