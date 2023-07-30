using System.Security.Cryptography;
using System.Text;

namespace Villainous.Server.Game;

public class UserContext
{
    private readonly List<User> _users = new();
    private readonly List<UserCredentials> _userCredentials = new();
    
    public Guid Register(string username, string password)
    {
        if (_userCredentials.Any(x => x.Username == username))
            return _userCredentials.Single(x => x.Username == username).UserId;
            //throw new Exception("This username is already taken");

        var salt = RandomNumberGenerator.GetBytes(64);
        var newPasswordHash = CaculateHash(password, salt);

        var id = Guid.NewGuid();
        _userCredentials.Add(new UserCredentials { UserId = id, Username = username, PasswordSalt = salt, PasswordHash = newPasswordHash });
        lock (_users)
        {
            _users.Add(new User { Id = id, DisplayName = username });
        }

        return id;
    }

    public Guid Login(string username, string password)
    {
        var userCredentials = _userCredentials.Single(x => x.Username == username);
        var passwordHash = CaculateHash(password, userCredentials.PasswordSalt);
        if (passwordHash != userCredentials.PasswordHash)
            throw new Exception("Password doesn't match");

        return userCredentials.UserId;
    }

    private static string CaculateHash(string password, byte[] salt) => Convert.ToHexString(Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, 1000, HashAlgorithmName.SHA512, 64));

    public User GetUser(Guid id)
    {
        return _users.Single(x => x.Id == id);
    }
}