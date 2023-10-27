﻿using System;

namespace FreeSql.SharingCore.MultiDatabase.Model
{
    public class TransactionsResult
    {
        public string Key { get; set; }
        public bool Successful { get; set; }
        public Exception Exception  { get; set; }
    }
}