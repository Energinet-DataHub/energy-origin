namespace API.Models;

public interface IEntity<out TId>
{
    TId Id { get; }
}
