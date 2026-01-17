using System;
using System.Collections.Generic;
using UserManagement.Core.Enums;

namespace UserManagement.Core.Entities
{
    public class User : Entity
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NormalizedEmail { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public UserStatus Status { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public int Version { get; set; } = 1;

        // Audit & Consent
        public string TermsVersion { get; set; } = string.Empty;
        public bool MarketingConsent { get; set; }
        public string RegistrationIp { get; set; } = string.Empty;
    }
}
