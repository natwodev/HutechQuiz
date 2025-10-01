using System.Collections.Generic;

namespace frontend_manage.DTOs.Common
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}

