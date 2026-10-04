namespace MyApp.Core.Entities;

public interface IEntity<TKey> where TKey : struct
{
    TKey Id { get; init; }
}

public interface IEntity : IEntity<int> { }
