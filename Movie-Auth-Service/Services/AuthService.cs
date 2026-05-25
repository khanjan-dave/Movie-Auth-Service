using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Movie_Auth_Service.Data;
using Movie_Auth_Service.DTOs;
using Movie_Auth_Service.Exceptions;
using Movie_Auth_Service.Models;

namespace Movie_Auth_Service.Services
{
    public class AuthService : IAuthService
    {
        private readonly AuthDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            AuthDbContext context,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<RegisterResponseDto> Register(RegisterDto request)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ValidationException("Email is required");

            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ValidationException("Password is required");

            if (request.Password.Length < 6)
                throw new ValidationException("Password must be at least 6 characters");

            if (string.IsNullOrWhiteSpace(request.FullName))
                throw new ValidationException("Full name is required");

            // Check duplicate email
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

            if (existingUser != null)
                throw new ConflictException("Email is already registered");

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                FullName = request.FullName.Trim(),
                Email = request.Email.ToLower().Trim(),
                PasswordHash = passwordHash,
                Role = "User"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New user registered: {Email}", user.Email);

            return new RegisterResponseDto
            {
                Message = "Registration successful! Please login to continue.",
                Email = user.Email
            };
        }

        public async Task<AuthResponseDto> Login(LoginDto request)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ValidationException("Email is required");

            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ValidationException("Password is required");

            // Find user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

            // Same error for wrong email OR wrong password (security best practice)
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedException("Invalid email or password");

            // Generate token
            var expiresAt = DateTime.UtcNow.AddHours(
                double.Parse(_configuration["Jwt:ExpiryHours"]!)
            );

            var token = GenerateJwtToken(user, expiresAt);

            _logger.LogInformation("User logged in: {Email}", user.Email);

            return new AuthResponseDto
            {
                Token = token,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                ExpiresAt = expiresAt
            };
        }

        private string GenerateJwtToken(User user, DateTime expiresAt)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
            );

            var credentials = new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256
            );

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}