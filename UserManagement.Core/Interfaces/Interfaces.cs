using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserManagement.Core.Entities;

namespace UserManagement.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetByIdAsync(Guid id);
        Task<User> GetByEmailAsync(string normalizedEmail);
        Task<User> GetByUsernameAsync(string username);
        Task<User> GetByPhoneAsync(string phoneNumber);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task<(IEnumerable<User> Users, long TotalCount)> SearchAsync(string query, string status, string role, int page, int pageSize);
    }

    public interface IAuditService
    {
        Task LogActionAsync(Guid? userId, string action, string details, string ipAddress);
    }
}
