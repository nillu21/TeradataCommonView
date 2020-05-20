using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Api.Models.TDCommonView
{
    public class TDDatabase
    {
        public string Name { get; set; }
        public bool IsOpen { get; set; }
        public List<TDTable> Tables { get; set; }
    }
}