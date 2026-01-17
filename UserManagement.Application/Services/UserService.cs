using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserManagement.Application.DTOs;
using UserManagement.Application.Interfaces;
using UserManagement.Core.Entities;
using UserManagement.Core.Enums;
using UserManagement.Core.Interfaces;
using UserManagement.Core.Exceptions;

namespace UserManagement.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuditService _auditService;
        private readonly IRiskService _riskService;

        public UserService(
            IUserRepository userRepository, 
            IPasswordHasher passwordHasher, 
            IAuditService auditService, 
            IRiskService riskService)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _auditService = auditService;
            _riskService = riskService;
        }

        public async Task<Guid> RegisterAsync(RegisterUserDto dto, string ipAddress)
        {
            // 1. I/O / Persistence / Security Checks (Application Layer)
            if (await _riskService.IsRequestRiskyAsync(ipAddress, dto.Username))
                throw new Exception("Registration blocked due to risk assessment.");

            // 2. Domain Validation & Entity Creation via Core Function (Exceptions bubble up)
            User.ValidatePasswordStrength(dto.Password, dto.Email, dto.Username);

            var user = User.CreateNew(
                dto.Username,
                dto.FullName,
                dto.Email,
                NormalizeEmail(dto.Email),
                dto.PhoneNumber,
                _passwordHasher.HashPassword(dto.Password),
                dto.BirthDate,
                dto.TermsVersion,
                dto.MarketingConsent,
                ipAddress
            );

            // 3. Application-level Identity Uniqueness (I/O)
            if (await _userRepository.GetByEmailAsync(user.NormalizedEmail) != null)
                throw new Exception("Email already exists.");

            if (await _userRepository.GetByUsernameAsync(user.Username) != null)
                throw new Exception("Username already exists.");

            if (await _userRepository.GetByPhoneAsync(user.PhoneNumber) != null)
                throw new Exception("Phone number already exists.");

            await _userRepository.AddAsync(user);
            await _auditService.LogActionAsync(user.Id, "UserRegistered", "Success", ipAddress);

            return user.Id;
        }

        public async Task<UserProfileDto> GetProfileAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            return new UserProfileDto(
                user.Id, user.Username, user.FullName, MaskEmail(user.Email), MaskPhone(user.PhoneNumber), 
                user.Status, user.Roles, user.BirthDate, user.CreatedAt, user.Version);
        }

        public async Task UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found.");
            if (user.Version != dto.Version) throw new Exception("Concurrency conflict.");

            user.UpdateProfile(dto.FullName, dto.PhoneNumber);

            user.IncrementVersion();
            await _userRepository.UpdateAsync(user);
            await _auditService.LogActionAsync(user.Id, "ProfileUpdated", "Success", "System");
        }

        public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            if (!_passwordHasher.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
                throw new Exception("Incorrect current password.");

            User.ValidatePasswordStrength(dto.NewPassword, user.Email, user.Username);
            user.UpdatePassword(_passwordHasher.HashPassword(dto.NewPassword));

            user.IncrementVersion();
            await _userRepository.UpdateAsync(user);
            await _auditService.LogActionAsync(user.Id, "PasswordChanged", "Success", "System");
        }

        public async Task SetStatusAsync(Guid userId, SetStatusDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Reason)) throw new Exception("Reason is required.");
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            user.UpdateStatus(dto.Status);
            user.IncrementVersion();
            await _userRepository.UpdateAsync(user);
            await _auditService.LogActionAsync(user.Id, "StatusChanged", $"To: {dto.Status}", "Admin");
        }

        public async Task ManageRoleAsync(Guid userId, string role, bool isRevoke)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            if (isRevoke) user.RemoveRole(role);
            else user.AddRole(role);
            
            user.IncrementVersion();
            await _userRepository.UpdateAsync(user);
            await _auditService.LogActionAsync(user.Id, isRevoke ? "RoleRevoked" : "RoleAssigned", $"Role: {role}", "Admin");
        }

        public async Task<PagedResult<UserProfileDto>> SearchAsync(SearchCriteria criteria)
        {
            var (users, total) = await _userRepository.SearchAsync(
                criteria.Query ?? "", criteria.Status ?? "", criteria.Role ?? "", criteria.Page, criteria.PageSize);

            var dtos = users.Select(u => new UserProfileDto(
                u.Id, u.Username, u.FullName, MaskEmail(u.Email), MaskPhone(u.PhoneNumber), u.Status, u.Roles, u.BirthDate, u.CreatedAt, u.Version));

            return new PagedResult<UserProfileDto>(dtos, total, criteria.Page, criteria.PageSize);
        }

        private string NormalizeEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return string.Empty;
            var parts = email.ToLowerInvariant().Trim().Split('@');
            if (parts.Length != 2) return email.ToLowerInvariant();
            var local = parts[0];
            var domain = parts[1];
            if (domain == "gmail.com" && local.Contains("+")) local = local.Split('+')[0];
            return $"{local}@{domain}";
        }

        private string MaskEmail(string email) => string.IsNullOrEmpty(email) ? "****" : $"{email[0]}****@{email.Split('@')[1]}";
        private string MaskPhone(string phone) => string.IsNullOrEmpty(phone) ? "****" : $"+****{phone.Substring(Math.Max(0, phone.Length - 4))}";
    }
}
