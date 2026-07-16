using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class PageResult<T>
    {
        public int Page {  get; set; }
        public int PageSize {  get; set; }
        public int? Total { get; set; }
        public List<T> Data { get; set; } = new();
    }
}
