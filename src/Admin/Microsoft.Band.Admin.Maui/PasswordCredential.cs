#if !WINDOWS

namespace Windows.Security.Credentials;

internal class PasswordCredential
{
    public string Resource { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
}

#endif
