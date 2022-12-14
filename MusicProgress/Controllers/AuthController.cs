using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MusicProgress.Data;
using MusicProgress.Models;
using MusicProgress.Services.Interfaces;

namespace MusicProgress.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;
        private readonly ITokenService _tokenService;
        public AuthController(IUserService service, IAuthService authService, ITokenService tokenService)
        {
            _userService = service;
            _authService = authService;
            _tokenService = tokenService;
        }

        [HttpPost("[action]")]
        public ActionResult<AuthData> Register(RegisterModel model)
        {
            var emailUniq = _userService.IsEmailUniq(model.Email);
            if (!emailUniq)
                return BadRequest(new {email = "email already exists"});
            var hashedPassword = _authService.HashPassword(model.Password);

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var user = new User
            {
                Email = model.Email,
                UserName = model.Username,
                HashedPassword = hashedPassword,
            };
            HttpContext.Response.Cookies.Append("refreshToken", token,
                new CookieOptions()
                {
                    HttpOnly = true,
                    Expires = DateTime.Now.AddDays(7)   
                });
            var userId = _userService.CreateUser(user);
            _tokenService.SetRefreshToken(new RefreshToken()
            {
                UserId = userId,
                Token = token,
                TimeCreated = DateTime.Now,
                TokenExpires = DateTime.Now.AddDays(7)
            });

            return _authService.GetToken(userId);
        }

        [HttpPost("[action]")]
        public ActionResult<LoginResult> Login(LoginModel model)
        {
            var user = _userService.GetByEmail(model.Email);
            if (user == null)
                return NotFound(new { email = "no user with this email"});
            var validPassword = _authService.VerifyPassword(model.Password, user.HashedPassword);
            if (!validPassword)
            {
                return BadRequest(new { password = "invalid password"});
            }

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            HttpContext.Response.Cookies.Append("refreshToken", token,
                new CookieOptions()
                {
                    HttpOnly = true,
                    Expires = DateTime.Now.AddDays(7),
                    SameSite = SameSiteMode.None,
                    Secure = true
                });
            _tokenService.SetRefreshToken(new RefreshToken()
            {
                UserId = user.UserId,
                Token = token,
                TimeCreated = DateTime.Now,
                TokenExpires = DateTime.Now.AddDays(7)
            });

            return new LoginResult()
            {
                AuthData = _authService.GetToken(user.UserId),
                UserInfo = new UserInfo() {Name = user.UserName}
            };
        }
        
        [HttpGet("refresh-token")]
        public ActionResult<LoginResult> RefreshToken()
        {
            var refreshToken = HttpContext.Request.Cookies["refreshToken"];
            var userId = _tokenService.GetUserIdByToken(refreshToken);
            if (userId == null)
            {
                return Unauthorized("Invalid Refresh Token");
            }

            var user = _userService.GetById((int) userId);
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            HttpContext.Response.Cookies.Append("refreshToken", token,
                new CookieOptions()
                {
                    HttpOnly = true,
                    Expires = DateTime.Now.AddDays(7)   
                });
            _tokenService.SetRefreshToken(new RefreshToken()
            {
                UserId = user.UserId,
                Token = token,
                TimeCreated = DateTime.Now,
                TokenExpires = DateTime.Now.AddDays(7)
            });
            return new LoginResult()
            {
                AuthData = _authService.GetToken(user.UserId),
                UserInfo = new UserInfo() {Name = user.UserName}
            };
        }
        
        [Authorize]
        [HttpGet("[action]")]
        public IActionResult LogOut()
        {
            var token = HttpContext.Request.Cookies["refreshToken"];
            _tokenService.RemoveRefreshToken(token);
            HttpContext.Response.Cookies.Delete("refreshToken");
            return Ok();
        }
    }
}