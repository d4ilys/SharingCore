using System;
using System.Collections.Generic;
using System.Text;

namespace SharingCore.MultiDatabase.Model
{
    public class QueryFuncResult<T>
    {
        public QueryFuncResult()
        {
        }
        public QueryFuncResult(List<T> result, long count)
        {
            Count = count;
            Result = result;
        }

        public long Count { get; set; } = 0;
        public List<T> Result { get; set; }
    }
}