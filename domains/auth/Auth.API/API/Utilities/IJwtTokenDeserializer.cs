namespace API.Services;

public interface IJwtDeserializer
{
    public T DeserializeJwt<T>(string token);
}
