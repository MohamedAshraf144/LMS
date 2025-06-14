using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Shared.Helpers
{
    
    
        public class PaginatedResult<T>
        {
            public IEnumerable<T> Items { get; set; } = new List<T>();
            public int TotalCount { get; set; }
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
            public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
            public bool HasPreviousPage => PageNumber > 1;
            public bool HasNextPage => PageNumber < TotalPages;
        }

        public static class PaginationHelper
        {
            public static PaginatedResult<T> CreatePaginatedResult<T>(
                IEnumerable<T> source,
                int pageNumber,
                int pageSize)
            {
                var totalCount = source.Count();
                var items = source
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return new PaginatedResult<T>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }
        }
    
}
