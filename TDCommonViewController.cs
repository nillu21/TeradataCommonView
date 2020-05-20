using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using TestTool.Utility;

namespace Client.Controllers
{
    public class TDCommonViewController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}