using WarungApp.Core.Common;
using WarungApp.Core.Entities;
using WarungApp.Core.Interfaces;

namespace WarungApp.Data;

public class CurrentUserContext : ICurrentUser
{
    public User? User { get; private set; }
    public bool IsLoggedIn => User is not null;
    public void Set(User user) => User = user;
    public void Clear() => User = null;
}
