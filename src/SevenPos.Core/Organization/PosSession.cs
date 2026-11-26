namespace SevenPos.Core.Organization;

/// <summary>
/// Who is logged in on this terminal. Registered as scoped: in the web host that means
/// one per browser connection; in the desktop app, one for the whole app.
/// </summary>
public class PosSession
{
    public User? CurrentUser { get; private set; }
    public bool IsSignedIn => CurrentUser is not null;

    public void SignIn(User user) => CurrentUser = user;
    public void SignOut() => CurrentUser = null;
}
