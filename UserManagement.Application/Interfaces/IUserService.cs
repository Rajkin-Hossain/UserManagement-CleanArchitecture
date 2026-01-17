using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserManagement.Core.Entities;
using UserManagement.Core.Enums;

namespace UserManagement.Application.DTOs
{
    public record RegisterUserDto(
        string Username,
        string FullName,
        string Email,
        string PhoneNumber,
        string Password,
        DateTime BirthDate,
        string TermsVersion,
        bool MarketingConsent
    );

    public record UserProfileDto(
        Guid Id,
        string Username,
        string FullName,
        string Email,
        string PhoneNumber,
        UserStatus Status,
        List<string> Roles,
        DateTime BirthDate,
        DateTime CreatedAt,
        int Version
    );

    public record UpdateProfileDto(
        string FullName,
        string PhoneNumber,
        int Version
    );

    public record ChangePasswordDto(
        string CurrentPassword,
        string NewPassword
    );

    public record SetStatusDto(
        UserStatus Status,
        string Reason
    );

    public record SearchCriteria(
        string? Query = null,
        string? Status = null,
        string? Role = null,
        int Page = 1,
        int PageSize = 20
    );

    public record PagedResult<T>(IEnumerable<T> Items, long TotalCount, int Page, int PageSize);
}
namespace UserManagement.Application.Interfaces
{
    using UserManagement.Application.DTOs;
    public interface IUserService
    {
        Task<Guid> RegisterAsync(RegisterUserDto dto, string ipAddress);
        Task<UserProfileDto> GetProfileAsync(Guid userId);
        Task UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
        Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
        Task SetStatusAsync(Guid userId, SetStatusDto dto);
        Task ManageRoleAsync(Guid userId, string role, bool isRevoke);
        Task<PagedResult<UserProfileDto>> SearchAsync(SearchCriteria criteria);
    }
}
