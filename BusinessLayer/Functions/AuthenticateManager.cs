using BusinessLayer.DTOs.Users;
using BusinessLayer.Models;
using BusinessLayer.Services;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Functions
{
    public interface IAuthenticateManager
    {
        Task<ResponceApi<Guid>> CreateUserByAdminAsync(CreateUserByAdminDTO dto);

        Task<ResponceApi<LoginResponceDTO>> LoginAsync(LoginDTO dto);

        Task<ResponceApi<LoginResponceDTO>> RefreshTokenAsync(RefreshTokenDTO dto);

        Task<ResponceApi<bool>> LogoutAsync(Guid userId, Guid sessionId);

        Task<ResponceApi<bool>> ChangePasswordAsync(ChangePasswordDTO dto);

        Task<ResponceApi<MyAccountDTO>> GetMyAccountAsync(Guid userId);

        Task<ResponceApi<List<UserListItemDTO>>> GetAllUsersAsync(Guid currentUserId);

        Task<ResponceApi<bool>> BlockUserAsync(Guid userId);

        Task<ResponceApi<bool>> UnBlockUserAsync(Guid userId);

        Task<ResponceApi<bool>> UpdateProfileAsync(UpdateProfileDTO dto);
    }

    public class AuthenticateManager : IAuthenticateManager
    {
        private readonly DBContext _db;
        private readonly IDataHasher _hasher;
        private readonly IDataCiphers _ciphers;
        private readonly ITokenSessionService _tokenSessionService;
        private readonly CairoTimeService _cairoTimeService;

        public AuthenticateManager(DBContext db, IDataHasher hasher, IDataCiphers ciphers, ITokenSessionService tokenSessionService, CairoTimeService cairoTimeService)
        {
            _db = db;
            _hasher = hasher;
            _ciphers = ciphers;
            _tokenSessionService = tokenSessionService;
            _cairoTimeService = cairoTimeService;
        }

        public async Task<ResponceApi<Guid>> CreateUserByAdminAsync(CreateUserByAdminDTO dto)
        {
            try
            {
                if (dto == null)
                    return ResponceApi<Guid>.Fail("Invalid user data");

                dto.FullName = dto.FullName.Trim();
                dto.UserName = dto.UserName.Trim().ToLower();
                dto.Password = dto.Password.Trim();

                if (string.IsNullOrWhiteSpace(dto.FullName) ||
                    string.IsNullOrWhiteSpace(dto.UserName) ||
                    string.IsNullOrWhiteSpace(dto.Password))
                {
                    return ResponceApi<Guid>.Fail("FullName, UserName and Password are required");
                }

                if (dto.Password != dto.ConfirmPassword)
                    return ResponceApi<Guid>.Fail("Password confirmation does not match");

                var usernameExists = await _db.Users.AnyAsync(u => u.UserName == dto.UserName);
                if (usernameExists)
                    return ResponceApi<Guid>.Fail("Username already exists");

                string? nationalIdEncrypted = null;
                string? nationalIdHash = null;
                string? phoneEncrypted = null;
                string? phoneHash = null;

                if (!string.IsNullOrWhiteSpace(dto.NationalId))
                {
                    var nationalId = dto.NationalId.Trim();
                    nationalIdEncrypted = _ciphers.Encrypt(nationalId);
                    nationalIdHash = _hasher.HashComparison(nationalId);

                    var nationalExists = await _db.Users.AnyAsync(u => u.NationalIdHash == nationalIdHash);
                    if (nationalExists)
                        return ResponceApi<Guid>.Fail("NationalId already exists");
                }

                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                {
                    var phone = dto.PhoneNumber.Trim();
                    phoneEncrypted = _ciphers.Encrypt(phone);
                    phoneHash = _hasher.HashComparison(phone);

                    var phoneExists = await _db.Users.AnyAsync(u => u.PhoneNumberHash == phoneHash);
                    if (phoneExists)
                        return ResponceApi<Guid>.Fail("Phone number already exists");
                }

                if (dto.Role == RulesAccount.Driver && dto.DriverId.HasValue)
                {
                    var driverExists = await _db.Drivers.AnyAsync(d => d.DriverId == dto.DriverId.Value);
                    if (!driverExists)
                        return ResponceApi<Guid>.Fail("Driver not found");
                }

                var user = new Users
                {
                    UserId = Guid.NewGuid(),
                    FullName = dto.FullName,
                    UserName = dto.UserName,
                    PasswordHash = _hasher.HashData(dto.Password),
                    RoleEncrypted = _ciphers.Encrypt(dto.Role.ToString()),
                    DriverId = dto.DriverId,
                    NationalIdEncrypted = nationalIdEncrypted,
                    NationalIdHash = nationalIdHash,
                    PhoneNumberEncrypted = phoneEncrypted,
                    PhoneNumberHash = phoneHash,
                    Login = false,
                    Blocked = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _db.Users.AddAsync(user);
                await _db.SaveChangesAsync();

                return ResponceApi<Guid>.Ok(user.UserId, "User created successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<Guid>.Fail("Error creating user", ex.Message);
            }
        }

        public async Task<ResponceApi<List<UserListItemDTO>>> GetAllUsersAsync(Guid currentUserId)
        {
            try
            {
                var firstAdminId = await _db.Users
                    .OrderBy(u => u.CreatedAt)
                    .Select(u => u.UserId)
                    .FirstOrDefaultAsync();

                var users = await _db.Users
                    .AsNoTracking()
                    .Where(u => u.UserId != firstAdminId
                             && u.UserId != currentUserId)
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();

                var result = users.Select(u => new UserListItemDTO
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    UserName = u.UserName,
                    DriverId = u.DriverId,
                    Login = u.Login,
                    Blocked = u.Blocked,
                    CreatedAt = _cairoTimeService.UtcToCairo(u.CreatedAt),
                    Role = string.IsNullOrWhiteSpace(u.RoleEncrypted)
                        ? null
                        : TryDecrypt(u.RoleEncrypted),
                    LastLoginAt = u.LastLogin.HasValue ? _cairoTimeService.UtcToCairo(u.LastLogin.Value) : null,
                }).ToList();

                return ResponceApi<List<UserListItemDTO>>.Ok(result, "Users retrieved successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<List<UserListItemDTO>>.Fail("Error retrieving users", ex.Message);
            }
        }

        private string? TryDecrypt(string cipherText)
        {
            try { return _ciphers.Decrypt(cipherText); }
            catch { return null; }
        }

        public async Task<ResponceApi<LoginResponceDTO>> LoginAsync(LoginDTO dto)
        {
            try
            {
                if (dto == null ||
                    string.IsNullOrWhiteSpace(dto.EmailOrPhoneOrUsernameOrNationalId) ||
                    string.IsNullOrWhiteSpace(dto.Password))
                {
                    return ResponceApi<LoginResponceDTO>.Fail("Username and password are required");
                }

                var input = dto.EmailOrPhoneOrUsernameOrNationalId.Trim().ToLower();
                var inputHash = _hasher.HashComparison(input);

                var user = await _db.Users.FirstOrDefaultAsync(u =>
                    u.UserName == input ||
                    u.NationalIdHash == inputHash ||
                    u.PhoneNumberHash == inputHash);

                if (user == null)
                    return ResponceApi<LoginResponceDTO>.Fail("Invalid username or password");

                if (user.Blocked)
                    return ResponceApi<LoginResponceDTO>.Fail("This account is blocked");

                var passwordValid = _hasher.VerifyHashed(dto.Password, user.PasswordHash);
                if (!passwordValid)
                    return ResponceApi<LoginResponceDTO>.Fail("Invalid username or password");

                user.Login = true;
                user.LastLogin = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                var sessionResult = await _tokenSessionService.CreateSessionAsync(user.UserId);
                if (!sessionResult.Success || sessionResult.Data == null)
                    return ResponceApi<LoginResponceDTO>.Fail("Unable to create session");

                var loginResponse = new LoginResponceDTO
                {
                    UserID = user.UserId,
                    SessionId = sessionResult.Data.SessionId,
                    Token = sessionResult.Data.AccessToken,
                    ExpireAt = sessionResult.Data.AccessTokenExpiresAt,
                    RefreshToken = sessionResult.Data.RefreshToken,
                    RefreshTokenExpireAt = sessionResult.Data.RefreshTokenExpiresAt
                };

                return ResponceApi<LoginResponceDTO>.Ok(loginResponse, "Login successful");
            }
            catch (Exception ex)
            {
                return ResponceApi<LoginResponceDTO>.Fail("Login failed", ex.Message);
            }
        }

        public async Task<ResponceApi<LoginResponceDTO>> RefreshTokenAsync(RefreshTokenDTO dto)
        {
            try
            {
                if (dto == null || dto.SessionId == Guid.Empty || string.IsNullOrWhiteSpace(dto.RefreshToken))
                    return ResponceApi<LoginResponceDTO>.Fail("Invalid refresh token request");

                var session = await _db.UserSessions
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.SessionId == dto.SessionId);

                if (session == null || !session.IsActive)
                    return ResponceApi<LoginResponceDTO>.Fail("Invalid session");

                if (session.RefreshTokenExpiresAt <= DateTime.UtcNow)
                {
                    session.IsActive = false;
                    await _db.SaveChangesAsync();
                    return ResponceApi<LoginResponceDTO>.Fail("Refresh token expired. Please login again");
                }

                if (session.User.Blocked)
                    return ResponceApi<LoginResponceDTO>.Fail("This account is blocked");

                var refreshValid = _hasher.VerifyHashed(dto.RefreshToken, session.RefreshTokenHash);
                if (!refreshValid)
                    return ResponceApi<LoginResponceDTO>.Fail("Invalid refresh token");

                var refreshResult = await _tokenSessionService.RefreshSessionAsync(
                    new RefreshTokenRequestDTO { SessionId = dto.SessionId, RefreshToken = dto.RefreshToken });

                if (!refreshResult.Success || refreshResult.Data == null)
                    return ResponceApi<LoginResponceDTO>.Fail(refreshResult.Message ?? "Unable to refresh token");

                session.LastActivityAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                var loginResponse = new LoginResponceDTO
                {
                    UserID = session.UserId,
                    SessionId = refreshResult.Data.SessionId,
                    Token = refreshResult.Data.AccessToken,
                    ExpireAt = refreshResult.Data.AccessTokenExpiresAt,
                    RefreshToken = refreshResult.Data.RefreshToken,
                    RefreshTokenExpireAt = refreshResult.Data.RefreshTokenExpiresAt
                };

                return ResponceApi<LoginResponceDTO>.Ok(loginResponse, "Token refreshed successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<LoginResponceDTO>.Fail("Refresh token failed", ex.Message);
            }
        }

        public async Task<ResponceApi<bool>> LogoutAsync(Guid userId, Guid sessionId)
        {
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null) return ResponceApi<bool>.Fail("User not found");

                var session = await _db.UserSessions.FirstOrDefaultAsync(s =>
                    s.SessionId == sessionId && s.UserId == userId);
                if (session == null) return ResponceApi<bool>.Fail("Session not found");

                session.IsActive = false;
                session.LastActivityAt = DateTime.UtcNow;

                var hasActiveSessions = await _db.UserSessions.AnyAsync(s =>
                    s.UserId == userId && s.SessionId != sessionId && s.IsActive);

                user.Login = hasActiveSessions;
                await _db.SaveChangesAsync();

                return ResponceApi<bool>.Ok(true, "Logout successful");
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail("Logout failed", ex.Message);
            }
        }

        public async Task<ResponceApi<bool>> ChangePasswordAsync(ChangePasswordDTO dto)
        {
            try
            {
                if (dto == null) return ResponceApi<bool>.Fail("Invalid password data");

                var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == dto.UserId);
                if (user == null) return ResponceApi<bool>.Fail("User not found");

                var oldPasswordValid = _hasher.VerifyHashed(dto.OldPassword, user.PasswordHash);
                if (!oldPasswordValid) return ResponceApi<bool>.Fail("Old password is wrong");

                if (dto.NewPassword != dto.ConfirmPassword)
                    return ResponceApi<bool>.Fail("Password confirmation does not match");

                user.PasswordHash = _hasher.HashData(dto.NewPassword);

                var sessions = await _db.UserSessions
                    .Where(s => s.UserId == user.UserId && s.IsActive)
                    .ToListAsync();

                foreach (var session in sessions)
                {
                    session.IsActive = false;
                    session.LastActivityAt = DateTime.UtcNow;
                }

                user.Login = false;
                await _db.SaveChangesAsync();

                return ResponceApi<bool>.Ok(true, "Password changed successfully. Please login again");
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail("Change password failed", ex.Message);
            }
        }

        public async Task<ResponceApi<MyAccountDTO>> GetMyAccountAsync(Guid userId)
        {
            try
            {
                var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null) return ResponceApi<MyAccountDTO>.Fail("User not found");

                var account = new MyAccountDTO
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    UserName = user.UserName,
                    Login = user.Login,
                    Blocked = user.Blocked,
                    CreatedAt = _cairoTimeService.UtcToCairo(user.CreatedAt),
                    DriverId = user.DriverId,
                    Role = string.IsNullOrWhiteSpace(user.RoleEncrypted)
                        ? null : TryDecrypt(user.RoleEncrypted),
                    NationalId = string.IsNullOrWhiteSpace(user.NationalIdEncrypted)
                        ? null : TryDecrypt(user.NationalIdEncrypted),
                    PhoneNumber = string.IsNullOrWhiteSpace(user.PhoneNumberEncrypted)
                        ? null : TryDecrypt(user.PhoneNumberEncrypted),
                    LastLoginAt = user.LastLogin.HasValue ? _cairoTimeService.UtcToCairo(user.LastLogin.Value) : null,
                };

                return ResponceApi<MyAccountDTO>.Ok(account, "Account retrieved successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<MyAccountDTO>.Fail("Get account failed", ex.Message);
            }
        }

        public async Task<ResponceApi<bool>> BlockUserAsync(Guid userId)
        {
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null) return ResponceApi<bool>.Fail("User not found");

                user.Blocked = true;
                user.Login = false;

                var sessions = await _db.UserSessions
                    .Where(s => s.UserId == userId && s.IsActive)
                    .ToListAsync();

                foreach (var session in sessions)
                {
                    session.IsActive = false;
                    session.LastActivityAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();
                return ResponceApi<bool>.Ok(true, "User blocked successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail("Block user failed", ex.Message);
            }
        }

        public async Task<ResponceApi<bool>> UnBlockUserAsync(Guid userId)
        {
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null) return ResponceApi<bool>.Fail("User not found");

                user.Blocked = false;
                await _db.SaveChangesAsync();

                return ResponceApi<bool>.Ok(true, "User unblocked successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail("Unblock user failed", ex.Message);
            }
        }

        public async Task<ResponceApi<bool>> UpdateProfileAsync(UpdateProfileDTO dto)
        {
            try
            {
                if (dto == null || dto.UserId == Guid.Empty)
                    return ResponceApi<bool>.Fail("بيانات غير صالحة");

                var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == dto.UserId);
                if (user == null)
                    return ResponceApi<bool>.Fail("المستخدم غير موجود");

                // ── Full Name ──
                if (!string.IsNullOrWhiteSpace(dto.FullName))
                    user.FullName = dto.FullName.Trim();

                // ── National ID ──
                if (dto.NationalId != null) // null = لم يُرسل، "" = طلب مسحه
                {
                    var n = dto.NationalId.Trim();
                    if (n.Length == 0)
                    {
                        user.NationalIdEncrypted = null;
                        user.NationalIdHash = null;
                    }
                    else
                    {
                        var newHash = _hasher.HashComparison(n);
                        // تحقق من التكرار مع استثناء المستخدم الحالي
                        var duplicate = await _db.Users.AnyAsync(u =>
                            u.UserId != dto.UserId && u.NationalIdHash == newHash);
                        if (duplicate)
                            return ResponceApi<bool>.Fail("الرقم القومي مسجّل لمستخدم آخر");

                        user.NationalIdEncrypted = _ciphers.Encrypt(n);
                        user.NationalIdHash = newHash;
                    }
                }

                // ── Phone Number ──
                if (dto.PhoneNumber != null)
                {
                    var p = dto.PhoneNumber.Trim();
                    if (p.Length == 0)
                    {
                        user.PhoneNumberEncrypted = null;
                        user.PhoneNumberHash = null;
                    }
                    else
                    {
                        var newHash = _hasher.HashComparison(p);
                        var duplicate = await _db.Users.AnyAsync(u =>
                            u.UserId != dto.UserId && u.PhoneNumberHash == newHash);
                        if (duplicate)
                            return ResponceApi<bool>.Fail("رقم الهاتف مسجّل لمستخدم آخر");

                        user.PhoneNumberEncrypted = _ciphers.Encrypt(p);
                        user.PhoneNumberHash = newHash;
                    }
                }

                await _db.SaveChangesAsync();
                return ResponceApi<bool>.Ok(true, "تم تحديث البيانات بنجاح");
            }
            catch (Exception ex) { return ResponceApi<bool>.Fail("فشل تحديث البيانات", ex.Message); }
        }

    }
}
