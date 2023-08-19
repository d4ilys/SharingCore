using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SharingCore.Context
{
    public class TenantContext
    {
        static AsyncLocal<string> OverallTenant = new AsyncLocal<string>();

        public static void SetTenant(string tenant)
        {
            OverallTenant.Value = tenant;
        }

        public static string GetTenant()
        {
            return OverallTenant.Value;
        }
    }
}