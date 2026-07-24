using Application.DTOs.Auth;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Application.Features.Auth.Commands.ForgotPassword;

public sealed record UserForgotPasswordCommand(
    string Email) : IRequest<bool>;

public sealed class UserForgotPasswordCommandHandler : IRequestHandler<UserForgotPasswordCommand, bool>
{
    private readonly IUserService _service;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IConfiguration _configuration;

    public UserForgotPasswordCommandHandler(IUserService service, IEmailService email, IUserRepository userRepository, IPasswordResetTokenRepository pass, IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _service = service;
        _emailService = email;
        _userRepository = userRepository;
        _passwordResetTokenRepository = pass;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    //public Task<bool> Handle(
    //    UserForgotPasswordCommand request,
    //    CancellationToken cancellationToken)
    //{
    //    return _service.ForgotPasswordAsync(request.Email, cancellationToken);
    //}






    public async Task<bool> Handle(UserForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null)
        {
            Console.WriteLine($"[DEBUG] Không tìm thấy user với email: {request.Email}");
            return true;
        }


        //tạo toẹn và hash
        string rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        string tokenHash = HashToken(rawToken);

        string baseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";
        string resetLink = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}&email={Uri.EscapeDataString(user.Email)}";
        string emailBody = $"<h2>Yêu cầu đặt lại mật khẩu</h2><p><a href='{resetLink}'>Đặt lại mật khẩu</a></p>";

        // lưu token vào db
        var tokenEntity = new PasswordResetToken(user.UserId, user.Email, tokenHash, DateTime.UtcNow.AddHours(1));
        await _passwordResetTokenRepository.AddAsync(tokenEntity, cancellationToken);
        await _service.ForgotPasswordAsync(request.Email, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        //_ = Task.Run(() => _emailService.SendEmailAsync(user.Email, "Reset Password", emailBody), cancellationToken);
        await _emailService.SendEmailAsync(user.Email, "Reset Password", emailBody);
        return true;
    }

    //hash token
    private string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
