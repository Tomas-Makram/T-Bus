using BusinessLayer.Services;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace TBus
{
    public static class AdminSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<DBContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IDataHasher>();
            var cipher = scope.ServiceProvider.GetRequiredService<IDataCiphers>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                await db.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AdminSeeder: database migration failed.");
                throw;
            }

            var users = await db.Users.ToListAsync();

            bool adminExists = users.Any(u =>
                !string.IsNullOrWhiteSpace(u.RoleEncrypted) &&
                cipher.Decrypt(u.RoleEncrypted) == RulesAccount.Admin.ToString());

            if (adminExists)
            {
                logger.LogInformation("AdminSeeder: Admin account already exists — skipping.");
                return;
            }

            var section = config.GetSection("AdminSeed");

            string password = section["Password"] ?? "Admin@1234";
            string userName = section["UserName"] ?? "admin";
            string fullName = section["FullName"] ?? "System Administrator";
            string phone = section["Phone"] ?? "01000000000";
            string nationalId = section["NationalId"] ?? "00000000000000";

            userName = userName.Trim().ToLower();
            fullName = fullName.Trim();
            phone = phone.Trim();
            nationalId = nationalId.Trim();

            var usernameExists = await db.Users.AnyAsync(u => u.UserName == userName);

            if (usernameExists)
            {
                logger.LogWarning("AdminSeeder: Username {UserName} already exists but no Admin role found.", userName);
                return;
            }

            var phoneHash = hasher.HashComparison(phone);
            var nationalIdHash = hasher.HashComparison(nationalId);

            var phoneExists = await db.Users.AnyAsync(u => u.PhoneNumberHash == phoneHash);
            var nationalExists = await db.Users.AnyAsync(u => u.NationalIdHash == nationalIdHash);

            if (phoneExists || nationalExists)
            {
                logger.LogWarning("AdminSeeder: Phone or NationalId already exists. Admin was not created.");
                return;
            }

            var admin = new Users
            {
                UserId = Guid.NewGuid(),
                UserName = userName,
                FullName = fullName,

                PasswordHash = hasher.HashData(password),

                RoleEncrypted = cipher.Encrypt(RulesAccount.Admin.ToString()),

                PhoneNumberEncrypted = cipher.Encrypt(phone),
                PhoneNumberHash = phoneHash,

                NationalIdEncrypted = cipher.Encrypt(nationalId),
                NationalIdHash = nationalIdHash,

                DriverId = null,

                CreatedAt = DateTime.UtcNow,
                LastLogin = null,
                Login = false,
                Blocked = false
            };

            await db.Users.AddAsync(admin);
            await db.SaveChangesAsync();

            logger.LogWarning("AdminSeeder: Default Admin account created. UserName={UserName}. CHANGE THE PASSWORD immediately after first login!", userName);
        }
    }
}