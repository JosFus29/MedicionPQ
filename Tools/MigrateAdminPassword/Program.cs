using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MedicionPQ.Data;
using MedicionPQ.Services;

namespace MedicionPQ.Tools.MigrateAdminPassword
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            string? email = null;
            string? newPassword = null;

            if (args.Length >= 2)
            {
                email = args[0];
                newPassword = args[1];
            }

            if (string.IsNullOrEmpty(email))
            {
                Console.Write("Email of the user to migrate: ");
                email = Console.ReadLine();
            }

            if (string.IsNullOrEmpty(newPassword))
            {
                Console.Write("New password (will be hashed): ");
                newPassword = ReadPassword();
            }

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(newPassword))
            {
                Console.WriteLine("Email and password are required.");
                return 2;
            }

            string? appsettingsPath = FindAppSettings();
            if (appsettingsPath == null)
            {
                Console.WriteLine("Could not find appsettings.json in parent folders. Provide connection string via environment variable CONNECTION_STRING.");
            }

            var configBuilder = new ConfigurationBuilder();
            if (appsettingsPath != null) configBuilder.AddJsonFile(appsettingsPath, optional: false, reloadOnChange: false);
            configBuilder.AddEnvironmentVariables();
            var config = configBuilder.Build();

            var connectionString = config.GetConnectionString("DefaultConnection") ?? config["ConnectionStrings:DefaultConnection"] ?? Environment.GetEnvironmentVariable("CONNECTION_STRING");

            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("Connection string not found. Set ConnectionStrings:DefaultConnection in appsettings.json or provide env var CONNECTION_STRING.");
                return 3;
            }

            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(opts => opts.UseSqlServer(connectionString));
            services.AddScoped<IPasswordService, PasswordService>();

            var provider = services.BuildServiceProvider();

            using (var scope = provider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var pwdService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

                var user = await db.Usuarios.FirstOrDefaultAsync(u => u.correo == email);
                if (user == null)
                {
                    Console.WriteLine($"User with email '{email}' not found.");
                    return 4;
                }

                var hashed = pwdService.HashPassword(newPassword);
                user.contrasena = hashed;
                await db.SaveChangesAsync();

                Console.WriteLine("Password updated and hashed successfully for user: " + email);
            }

            return 0;
        }

        private static string? FindAppSettings()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            for (int i = 0; i < 8 && dir != null; i++)
            {
                var candidate = Path.Combine(dir.FullName, "appsettings.json");
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            return null;
        }

        private static string ReadPassword()
        {
            var pwd = string.Empty;
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;
                if (key == ConsoleKey.Backspace && pwd.Length > 0)
                {
                    pwd = pwd[..^1];
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    pwd += keyInfo.KeyChar;
                    Console.Write('*');
                }
            } while (key != ConsoleKey.Enter);
            Console.WriteLine();
            return pwd;
        }
    }
}
