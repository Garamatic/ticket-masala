using Microsoft.AspNetCore.Mvc.Rendering;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Web.Utilities;

/// <summary>
/// Provides extension methods for formatting user and entity names consistently.
/// Reduces duplication across the codebase by centralizing name formatting logic.
/// </summary>
public static class UserDisplayHelper
{
    /// <summary>
    /// Formats a user's full name as "FirstName LastName"
    /// </summary>
    public static string ToFullName(this ApplicationUser user)
        => $"{user.FirstName} {user.LastName}".Trim();

    /// <summary>
    /// Formats an employee's full name as "FirstName LastName"
    /// </summary>
    public static string ToFullName(this Employee employee)
        => $"{employee.FirstName} {employee.LastName}".Trim();

    /// <summary>
    /// Creates a SelectListItem for a user with full name as display text
    /// </summary>
    public static SelectListItem ToSelectListItem(this ApplicationUser user, string? selectedValue = null)
        => new()
        {
            Value = user.Id,
            Text = user.ToFullName(),
            Selected = selectedValue != null && user.Id == selectedValue
        };

    /// <summary>
    /// Creates a SelectListItem for an employee with full name as display text
    /// </summary>
    public static SelectListItem ToSelectListItem(this Employee employee, string? selectedValue = null)
        => new()
        {
            Value = employee.Id,
            Text = employee.ToFullName(),
            Selected = selectedValue != null && employee.Id == selectedValue
        };

    /// <summary>
    /// Converts a collection of users to SelectListItems
    /// </summary>
    public static IEnumerable<SelectListItem> ToSelectListItems(
        this IEnumerable<ApplicationUser> users,
        string? selectedValue = null)
        => users.Select(u => u.ToSelectListItem(selectedValue));

    /// <summary>
    /// Converts a collection of employees to SelectListItems
    /// </summary>
    public static IEnumerable<SelectListItem> ToSelectListItems(
        this IEnumerable<Employee> employees,
        string? selectedValue = null)
        => employees.Select(e => e.ToSelectListItem(selectedValue));

    /// <summary>
    /// Converts a collection of entities to SelectListItems with custom value and text selectors
    /// </summary>
    public static IEnumerable<SelectListItem> ToSelectListItems<T>(
        this IEnumerable<T> items,
        Func<T, string> valueSelector,
        Func<T, string> textSelector,
        string? selectedValue = null)
        => items.Select(item => new SelectListItem
        {
            Value = valueSelector(item),
            Text = textSelector(item),
            Selected = selectedValue != null && valueSelector(item) == selectedValue
        });
}
