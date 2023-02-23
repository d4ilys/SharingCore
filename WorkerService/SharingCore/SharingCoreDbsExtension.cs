using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharingCore.Common;
using SharingCore.MultiDatabase.Wrapper;

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
        public static string Business(this SharingCoreDbs dbs) => "sharingcore_business";

        /// <summary>
        /// 主业务库
        /// </summary>
        /// <param name="dbs"></param>
        /// <returns></returns>
        public static string Logs(this SharingCoreDbs dbs) => "sharingcore_log";

    }
}