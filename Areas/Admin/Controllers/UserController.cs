using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Equinox.Models;

namespace Equinox.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]/[action]/{id?}")]
    public class UserController : Controller
    {
        private readonly EquinoxContext _context;
        public UserController(EquinoxContext context) => _context = context;

        // LIST
        public IActionResult Index()
        {
            var users = _context.Users.ToList();
            return View(users);
        }

        // CREATE (GET)
        public IActionResult Create()
        {
            return View(new User());
        }

        // CREATE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(User user)
        {
            // Auto-generate UserId if not provided
            if (string.IsNullOrEmpty(user.UserId))
            {
                user.UserId = Guid.NewGuid().ToString().Substring(0, 8);
            }

            // Server-side validation for phone number uniqueness
            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                bool phoneExists = _context.Users.Any(u => u.PhoneNumber == user.PhoneNumber);
                if (phoneExists)
                {
                    ModelState.AddModelError("PhoneNumber", "Phone number already in use");
                }
            }

            // Server-side age validation
            if (user.DOB != default(DateTime))
            {
                var age = DateTime.Today.Year - user.DOB.Year;
                if (user.DOB.Date > DateTime.Today.AddYears(-age)) age--;
                
                if (age < 8 || age > 80)
                {
                    ModelState.AddModelError("DOB", "Age must be between 8 and 80");
                }
            }

            if (!ModelState.IsValid)
            {
                TempData["ValidationErrors"] = "Please fix the validation errors below";
                return View(user);
            }

            _context.Users.Add(user);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "User added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // EDIT (GET)
        [HttpGet]
        public IActionResult Edit(string id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // EDIT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, User model)
        {
            if (id != model.UserId)
                return BadRequest();

            // Server-side validation for phone number uniqueness
            if (!string.IsNullOrEmpty(model.PhoneNumber))
            {
                bool phoneExists = _context.Users.Any(u => u.PhoneNumber == model.PhoneNumber && u.UserId != model.UserId);
                if (phoneExists)
                {
                    ModelState.AddModelError("PhoneNumber", "Phone number already in use");
                }
            }

            // Server-side age validation
            if (model.DOB != default(DateTime))
            {
                var age = DateTime.Today.Year - model.DOB.Year;
                if (model.DOB.Date > DateTime.Today.AddYears(-age)) age--;
                
                if (age < 8 || age > 80)
                {
                    ModelState.AddModelError("DOB", "Age must be between 8 and 80");
                }
            }

            if (!ModelState.IsValid)
            {
                TempData["ValidationErrors"] = "Please fix the validation errors below";
                return View(model);
            }

            _context.Update(model);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "User updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // DELETE (GET)
        public IActionResult Delete(string id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // DELETE (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id)
        {
            return DeleteUser(id);
        }

        private IActionResult DeleteUser(string id)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "User deleted successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        // REMOTE VALIDATION
        [AcceptVerbs("GET", "POST")]
        public IActionResult VerifyPhone(string phoneNumber, string userId)
        {
            bool taken = _context.Users.Any(u => u.PhoneNumber == phoneNumber
                                          && u.UserId != userId);
            return taken
                ? Json($"Phone {phoneNumber} already in use")
                : Json(true);
        }
    }
}
