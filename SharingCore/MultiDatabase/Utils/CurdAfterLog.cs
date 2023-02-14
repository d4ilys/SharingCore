using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using SharingCore.Assemble.Model;

namespace SharingCore.MultiDatabase.Utils
{
    public class CurdAfterLog
    {
        public static AsyncLocal<string> CurrentLog = new AsyncLocal<string>();
    }
}