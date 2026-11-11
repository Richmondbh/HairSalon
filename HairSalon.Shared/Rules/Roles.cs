namespace HairSalon.Shared.Rules;

/// <summary>
/// Role names in one place. Using constants instead of typing "Admin" everywhere
/// means a typo becomes a compile error instead of a security bug.
/// They are "const" so they can be used inside attributes, e.g. [Authorize(Roles = Roles.Admin)].
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";
}
