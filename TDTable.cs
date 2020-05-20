 using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Api.Models.TDCommonView
{
    public class TDTable
    {
        public string Database { get; set; }
        public string Name { get; set; }
        public int ColumntCount { get; set; }

        public bool isOpen { get; set; }
    }
}