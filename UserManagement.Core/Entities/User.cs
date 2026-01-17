using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UserManagement.Core.Enums;
using UserManagement.Core.Exceptions;

namespace UserManagement.Core.Entities
{
    public class User : Entity
    {
        public string Username { get; private set; } = string.Empty;
        public string FullName { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string NormalizedEmail { get; private set; } = string.Empty;
        public string PhoneNumber { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public DateTime BirthDate { get; private set; }
        public string TermsVersion { get; private set; } = string.Empty;
        public UserStatus Status { get; private set; }
        public List<string> Roles { get; private set; } = new List<string>();
        public int Version { get; private set; } = 1;
        public bool MarketingConsent { get; private set; }
        public string RegistrationIp { get; private set; } = string.Empty;

        // Factory-like function for initial registration
        public static User CreateNew(
            string username, 
            string fullName, 
            string email, 
            string normalizedEmail,
            string phoneNumber, 
            string passwordHash, 
            DateTime birthDate, 
            string termsVersion, 
            bool marketingConsent, 
            string registrationIp)
        {
            var user = new User();
            
            user.SetUsername(username);
            user.SetFullName(fullName);
            user.SetEmail(email);
            user.SetPhoneNumber(phoneNumber);
            user.SetBirthDate(birthDate);
            user.SetTerms(termsVersion);
            
            user.NormalizedEmail = normalizedEmail;
            user.PasswordHash = passwordHash;
            user.MarketingConsent = marketingConsent;
            user.RegistrationIp = registrationIp;
            user.Status = UserStatus.PendingVerification;
            
            return user;
        }

        public void UpdateProfile(string fullName, string phoneNumber)
        {
            SetFullName(fullName);
            SetPhoneNumber(phoneNumber);
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdatePassword(string newHash)
        {
            if (string.IsNullOrEmpty(newHash)) throw new DomainException("Password hash cannot be empty.");
            PasswordHash = newHash;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateStatus(UserStatus status)
        {
            Status = status;
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role)) throw new DomainException("Role cannot be empty.");
            if (!Roles.Contains(role)) Roles.Add(role);
            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveRole(string role)
        {
            if (Roles.Remove(role)) UpdatedAt = DateTime.UtcNow;
        }

        public void IncrementVersion() => Version++;

        // Private Validation Logic
        private void SetUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 20)
                throw new DomainException("Username must be between 3 and 20 characters.");

            var reserved = new[] { "admin", "support", "system", "root" };
            if (reserved.Contains(username.ToLower()))
                throw new DomainException("Username is reserved.");
            
            Username = username;
        }

        private void SetFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName) || fullName.Length < 2 || fullName.Length > 80)
                throw new DomainException("Full name must be between 2 and 80 characters.");

            if (!Regex.IsMatch(fullName, @"^[a-zA-Z\s]*$"))
                throw new DomainException("Full name must not contain symbols or digits.");
            
            FullName = fullName;
        }

        private void SetEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email cannot be empty.");
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new DomainException("Invalid email format.");
            Email = email;
        }

        private void SetPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || !Regex.IsMatch(phoneNumber, @"^\+?[1-9]\d{1,14}$"))
                throw new DomainException("Invalid E.164 phone format.");

            if (phoneNumber.StartsWith("+880"))
            {
                if (phoneNumber.Length != 14) throw new DomainException("Invalid Bangladesh phone length.");
                var prefix = phoneNumber.Substring(4, 3);
                if (!new[] { "171", "181", "191", "161", "131", "141", "151" }.Contains(prefix))
                    throw new DomainException("Invalid operator prefix for Bangladesh.");
            }
            PhoneNumber = phoneNumber;
        }

        private void SetBirthDate(DateTime birthDate)
        {
            var today = DateTime.UtcNow.Date;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            if (age < 13) throw new DomainException("User must be at least 13 years old.");
            BirthDate = birthDate;
        }

        private void SetTerms(string termsVersion)
        {
            if (string.IsNullOrWhiteSpace(termsVersion)) throw new DomainException("Terms version is required.");
            TermsVersion = termsVersion;
        }

        public static void ValidatePasswordStrength(string password, string email, string username)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
                throw new DomainException("Password must be at least 12 characters.");

            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSymbol = password.Any(c => !char.IsLetterOrDigit(c));

            if ((hasUpper ? 1 : 0) + (hasLower ? 1 : 0) + (hasDigit ? 1 : 0) + (hasSymbol ? 1 : 0) < 3)
                throw new DomainException("Password must include 3 of 4: upper, lower, digit, symbol.");

            if (password.Contains(username) || password.Contains(email?.Split('@')[0] ?? ""))
                throw new DomainException("Password cannot contain username or email parts.");
        }
    }
}
