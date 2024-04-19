using System;
using System.Collections.Generic;

namespace API.Data;

public interface IRepository<T> where T : class
{
    T? Get(Guid id);
    IEnumerable<T> GetAll();
    void Add(T entity);
    void Remove(T entity);
    void Update(T entity);
}

