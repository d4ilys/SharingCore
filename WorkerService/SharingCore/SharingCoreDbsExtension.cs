using FreeSql.SharingCore.MultiDatabase.Wrapper;

namespace TSP.WokerServices.Base
{
    public static class SharingCoreDbsExtension
    {
        /// <summary>
        /// 基础库
        /// </summary>
        /// <param name="dbs"></param>
        /// <returns></returns>
        public static string Basics(this SharingCoreDbs dbs) => "sharingcore_basics";

        /// <summary>
        /// 主业务库
        /// </summary>
        /// <param name="dbs"></param>
        /// <returns></returns>
        [Database(Name = "sharingcore_business_{yyyy}", Separate = "createtime=2022-01-01(1 year)")]
        public static string Business(this SharingCoreDbs dbs) => "sharingcore_business";
        

        /// <summary>
        /// 日志库
        /// </summary>
        /// <param name="dbs"></param>
        /// <returns></returns>
        public static string Logs(this SharingCoreDbs dbs) => "sharingcore_log";

    }
}