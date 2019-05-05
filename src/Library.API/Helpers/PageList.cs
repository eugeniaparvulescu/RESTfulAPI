using System;
using System.Collections.Generic;
using System.Linq;

namespace Library.API.Helpers
{
    public class PageList<T> : List<T>
    {
        public int CurrentPage { get; private set; }
        public int PageSize { get; private set; }
        public int TotalPages { get; private set; }
        public int TotalCount { get; private set; }

        public bool HasPrevious { get { return CurrentPage > 1; } }
        public bool HasNext { get { return CurrentPage < TotalPages; } }

        public PageList(List<T> items, int count, int pageNumber, int pageSize)
        {
            CurrentPage = pageNumber;
            PageSize = pageSize;
            TotalCount = count;
            TotalPages = (int)Math.Ceiling(TotalCount / (decimal)PageSize);
            AddRange(items);
        }

        public static PageList<T> Create(IQueryable<T> source, int pageNumber, int pageSize)
        {
            var count = source.Count();
            var items = source
                .Skip(pageSize * (pageNumber - 1))
                .Take(pageSize)
                .ToList();

            return new PageList<T>(items, count, pageNumber, pageSize);
        }
    }
}
