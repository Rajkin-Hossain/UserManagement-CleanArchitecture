using System.Threading.Tasks;

namespace UserManagement.Application.Interfaces
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    public interface IRiskService
    {
        Task<bool> IsRequestRiskyAsync(string ipAddress, string username);
    }
}
