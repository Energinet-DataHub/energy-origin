using API.Models;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;



namespace API.Controllers;


[ApiController]
public class AuthController : ControllerBase
{
    readonly ILogger<AuthController> logger;
    readonly IOidcProviders oidcProviders;

    public AuthController(ILogger<AuthController> logger, IOidcProviders oidcProviders)
    {
        this.logger = logger;
        this.oidcProviders = oidcProviders;
    }

    [HttpGet]
    [Route("/oidc/login")]
    public NextStep Login(
        [Required] string feUrl,
        [Required] string returnUrl)
    {
        AuthState state = new AuthState()
        {
            FeUrl = feUrl,
            ReturnUrl = returnUrl
        };

        return oidcProviders.CreateAuthorizationUri(state);
    }

    [HttpPost]
    [Route("test/makeCookie")]
    public ActionResult<LogoutResponse> TestLogin()
    {

        CookieOptions cookieOptions = new CookieOptions
        {
            Path = "/",
            Domain = "localhost",
            HttpOnly = true,
            //SameSite = 0,
            Secure = true,
            Expires = new DateTimeOffset(DateTime.Now.AddDays(7))
        };
        HttpContext.Response.Cookies.Append("Authorization", "ee", cookieOptions);

        return Ok();
    }




    [HttpPost]
    [Route("auth/oidc/logout")]
    public ActionResult<LogoutResponse> Logout()
    {

        //1. fecth token -> valid opaque_token
        //2. if token -> delete token from database, logout from oidc
        //3. Set cookie with new values
        //4. give response -> success

        var token = "";
        try
        {
            //var token = fetchToken();
            //token = "Insert token here";
            var context = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            token = context;
        }
        catch (Exception)
        {

            throw new NotImplementedException($"fetchToken() is not yet implemented.");
        }

        if (token != null)
        {
            //try token from DB
            //call oidc logout
            //commmit
            var test = "";
        }


        var cookie = HttpContext.Request.Cookies["Authorization"];



        return Ok(new LogoutResponse { success = true });
    }

}
