using LMS.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Core.Interfaces.Services
{
    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User> CreateUserAsync(User user);
        Task<bool> ValidateUserCredentialsAsync(string email, string password);
        Task<string> GenerateJwtTokenAsync(User user);
    }
}
