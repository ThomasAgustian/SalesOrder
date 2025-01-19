using System.Collections.Generic;
using System.Security.Policy;
using System;
using System.Web.UI.WebControls;
using SalesOrder.Models;

namespace SalesOrder.Controllers
{
    public class SOOrderViewModel
    {
        public List<Customer> SO_ORDER { get; set; }
        public List<Customer> SO_ITEM { get; set; }
    }
}