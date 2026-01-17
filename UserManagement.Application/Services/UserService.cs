using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using UserManagement.Application.DTOs;
using UserManagement.Application.Interfaces;
using UserManagement.Core.Entities;
using UserManagement.Core.Enums;
using UserManagement.Core.Interfaces;

namespace UserManagement.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuditService _auditService;
        private readonly IRiskService _riskService;
        private readonly IValidator<RegisterUserDto> _registerValidator;

        public UserService(
            IUserRepository userRepository, 
            IPasswordHasher passwordHasher, 
            IAuditService auditService, 
            IRiskService riskService,
            IValidator<RegisterUserDto> registerValidator)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _auditService = auditService;
            _riskService = riskService;
            _registerValidator = registerValidator;
        }

        public async Task<Guid> RegisterAsync(RegisterUserDto dto, string ipAddress)
        {
            var validationResult = await _registerValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            if (await _riskService.IsRequestRiskyAsync(ipAddress, dto.Username))
                throw new Exception("Registration blocked due to risk assessment.");

            var normalizedEmail = NormalizeEmail(dto.Email);

            if (await _userRepository.GetByEmailAsync(normalizedEmail) != null)
                throw new Exception("Email already exists.");

            if (await _userRepository.GetByUsernameAsync(dto.Username) != null)
                throw new Exception("Username already exists.");

            if (await _userRepository.GetByPhoneAsync(dto.PhoneNumber) != null)
                throw new Exception("Phone number already exists.");

            var user = new User
            {
                Username = dto.Username,
                FullName = dto.FullName,
                Email = dto.Email,
                NormalizedEmail = normalizedEmail,
                PhoneNumber = dto.PhoneNumber,
                PasswordHash = _passwordHasher.HashPassword(dto.Password),
                BirthDate = dto.BirthDate,
                TermsVersion = dto.TermsVersion,
                MarketingConsent = dto.MarketingConsent,
                RegistrationIp = ipAddress,
                Status = UserStatus.PendingVerification
            };

            await _userRepository.AddAsync(user);
            await _auditService.LogActionAsync(user.Id, "UserRegistered", $"User {user.Username} registered.", ipAddress);

            return user.Id;
        }

        public async Task<UserProfileDto> GetProfileAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            return new UserProfileDto(
                user.Id,
                user.Username,
                user.FullName,
                MaskEmail(user.Email),
                MaskPhone(user.PhoneNumber),
                user.Status,
                user.Roles,
                user.BirthDate,
                user.CreatedAt,
                user.Version
            );
        }

        public async Task UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            if (user.Version != dto.Version)
                throw new Exception("Concurrency conflict.");

            var oldName = user.FullName;
            var oldPhone = user.PhoneNumber;

            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;
            user.Version++;

            await _userRepository.UpdateAsync(user);
            await _auditService.LogActionAsync(user.Id, "ProfileUpdated", 
                $"Changed Name from '{oldName}' to '{dto.FullName}', Phone from '{oldPhone}' to '{dto.PhoneNumber}'", "System");
        }

        public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            if (!_passwordHasher.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
                throw new Exception("Incorrect current password.");

            user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);
            user.Version++;

            await _userRepository.UpdateAsync(user);
            await _auditService.LogActionAsync(user.Id, "PasswordChanged", "User successfully changed password.", "System");
        }

        public async Task SetStatusAsync(Guid userId, SetStatusDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new Exception("Reason is required.");

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            var oldStatus = user.Status;
            user.Status = dto.Status;
            user.Version++;

            await _userRepository.UpdateAsync(user);
            await _auditService.LogActionAsync(user.Id, "StatusChanged", 
                $"Status changed from {oldStatus} to {dto.Status}. Reason: {dto.Reason}", "Admin");
        }

        public async Task ManageRoleAsync(Guid userId, string role, bool isRevoke)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            if (isRevoke) user.Roles.Remove(role);
            else if (!user.Roles.Contains(role)) user.Roles.Add(role);
            
            user.Version++;
            await _userRepository.UpdateAsync(user);
            await _auditService.LogActionAsync(user.Id, isRevoke ? "RoleRevoked" : "RoleAssigned", $"Role {role}", "Admin");
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
            var parts = email.ToLowerInvariant().Trim().Split('@');
            if (parts.Length != 2) return email.ToLowerInvariant();
            var local = parts[0];
            var domain = parts[1];
            if (domain == "gmail.com" && local.Contains("+")) local = local.Split('+')[0];
            return $"{local}@{domain}";
        }

        private string MaskEmail(string email) => $"{email[0]}****@{email.Split('@')[1]}";
        private string MaskPhone(string phone) => $"+****{phone.Substring(Math.Max(0, phone.Length - 4))}";
    }
}
