using System;
using System.Threading.Tasks;

namespace DataWizard.UI.Services
{
    public class AuthenticationService
    {
        private readonly DatabaseService _dbService;

        public AuthenticationService()
        {
            _dbService = new DatabaseService();
        }

        public async Task<(bool success, string error)> SignInAsync(string username, string password)
        {
            return await _dbService.ValidateUserCredentialsAsync(username, password);
        }

        public async Task<(bool success, string error)> SignUpAsync(string username, string password, string email, string fullName = null)
        {
            return await _dbService.CreateUserAsync(username, password, email, fullName);
        }
    }
}