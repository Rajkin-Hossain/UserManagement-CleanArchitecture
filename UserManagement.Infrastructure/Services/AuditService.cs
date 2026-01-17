using System;
using System.Threading.Tasks;
using UserManagement.Core.Interfaces;
using UserManagement.Infrastructure.Persistence;

namespace UserManagement.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly MongoDbContext _context;

        public AuditService(MongoDbContext context)
        {
            _context = context;
        }

        public async Task LogActionAsync(Guid? userId, string action, string details, string ipAddress)
        {
            var entry = new AuditEntry
            {
                UserId = userId,
                Action = action,
                Details = details,
                IpAddress = ipAddress
            };
            await _context.AuditLogs.InsertOneAsync(entry);
        }
    }
}
