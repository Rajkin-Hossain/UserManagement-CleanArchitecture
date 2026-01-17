using System.Threading.Tasks;
using UserManagement.Application.Interfaces;

namespace UserManagement.Infrastructure.Services
{
    public class RiskService : IRiskService
    {
        public Task<bool> IsRequestRiskyAsync(string ipAddress, string username)
        {
            // Simplified risk logic: block if IP is local (for demo) or certain name
            if (ipAddress == "127.0.0.1" && username.Contains("fraud"))
                return Task.FromResult(true);

            return Task.FromResult(false);
        }
    }
}
