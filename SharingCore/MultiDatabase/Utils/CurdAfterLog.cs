using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Daily.SharingCore.Assemble.Model;

namespace Daily.SharingCore.MultiDatabase.Utils
{
    public class CurdAfterLog
    {
        public static AsyncLocal<string> CurrentLog = new AsyncLocal<string>();
    }
}