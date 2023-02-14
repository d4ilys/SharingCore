using FreeSql.DataAnnotations;
using System;
using System.Numerics;

namespace SharingCore.MultiDatabase.Model
{
    /// <summary>
    /// 记录事务执行的日志
    /// </summary>
    public class multi_transaction_log
    {
        [Column(IsIdentity = true)]
        public long id
        {
            get; set;
        }
        /// <summary>
        /// 事务内容
        /// </summary>
        [Column(StringLength = 1000)]
        public string? content
        {
            get; set;
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime create_time { get; set; } = DateTime.Now;

        /// <summary>
        /// 执行SQL
        /// </summary>
        [Column(StringLength = 1000)]
        public string? exec_sql
        {
            get; set;
        }

        /// <summary>
        /// 执行结果
        /// </summary>
        public string? result_msg
        {
            get; set;
        }

        /// <summary>
        /// 是否成功 0成功、1失败
        /// </summary>
        public int successful { get; set; } = 0;
    }
}
