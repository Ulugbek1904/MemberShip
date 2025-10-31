using System.Collections;

namespace Common.EF.Generics;

public sealed class PagedCollection<T>(IEnumerable<T> items, int total) : IEnumerable<T>, IEnumerable
{
    public IEnumerable<T> Items { get; set; } = items;

    public int Total { get; set; } = total;

    public IEnumerator<T> GetEnumerator()
    {
        return Items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
