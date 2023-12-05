using FreeSql.SharingCore;
using FreeSql.SharingCore.MultiDatabase.Wrapper;

namespace TSP.WokerServices.Base
{
    public static class SharingCoreDbsExtension
    {
        /// <summary>
        /// 日志
        /// </summary>
        /// <param name="dbs"></param>
        /// <returns></returns>
        public static string Log(this SharingCoreDbs dbs) => "logs";

        /// <summary>
        /// 订单库
        /// </summary>
        /// <param name="dbs"></param>
        /// <returns></returns>
        public static string Order(this SharingCoreDbs dbs) => "order";

    }
}