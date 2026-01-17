using MongoDB.Driver;
using UserManagement.Core.Entities;
using UserManagement.Core.Interfaces;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UserManagement.Infrastructure.Persistence
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
        public IMongoCollection<AuditEntry> AuditLogs => _database.GetCollection<AuditEntry>("AuditLogs");
    }

    public class AuditEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? UserId { get; set; }
        public string? Action { get; set; }
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class UserRepository : IUserRepository
    {
        private readonly MongoDbContext _context;

        public UserRepository(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetByIdAsync(Guid id) => 
            await _context.Users.Find(u => u.Id == id).FirstOrDefaultAsync();

        public async Task<User> GetByEmailAsync(string normalizedEmail) => 
            await _context.Users.Find(u => u.NormalizedEmail == normalizedEmail).FirstOrDefaultAsync();

        public async Task<User> GetByUsernameAsync(string username) => 
            await _context.Users.Find(u => u.Username.ToLower() == username.ToLower()).FirstOrDefaultAsync();

        public async Task<User> GetByPhoneAsync(string phoneNumber) => 
            await _context.Users.Find(u => u.PhoneNumber == phoneNumber).FirstOrDefaultAsync();

        public async Task AddAsync(User user) => await _context.Users.InsertOneAsync(user);

        public async Task UpdateAsync(User user)
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Id, user.Id),
                Builders<User>.Filter.Eq(u => u.Version, user.Version - 1) // Optimistic concurrency
            );
            var result = await _context.Users.ReplaceOneAsync(filter, user);
            if (result.ModifiedCount == 0) throw new Exception("Concurrency conflict.");
        }

        public async Task<(IEnumerable<User> Users, long TotalCount)> SearchAsync(string query, string status, string role, int page, int pageSize)
        {
            var filterBuilder = Builders<User>.Filter;
            var filter = filterBuilder.Empty;

            if (!string.IsNullOrEmpty(query))
                filter &= filterBuilder.Or(
                    filterBuilder.Regex(u => u.Username, new MongoDB.Bson.BsonRegularExpression(query, "i")),
                    filterBuilder.Regex(u => u.FullName, new MongoDB.Bson.BsonRegularExpression(query, "i"))
                );

            if (!string.IsNullOrEmpty(status))
                filter &= filterBuilder.Eq(u => u.Status.ToString(), status);

            if (!string.IsNullOrEmpty(role))
                filter &= filterBuilder.AnyEq(u => u.Roles, role);

            var totalCount = await _context.Users.CountDocumentsAsync(filter);
            var users = await _context.Users.Find(filter)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (users, totalCount);
        }
    }
}
