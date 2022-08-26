namespace API.Services;
public interface ICookies
{
    CookieOptions CreateCookieOptions(int cookieExpiresTimeDelta);
}
