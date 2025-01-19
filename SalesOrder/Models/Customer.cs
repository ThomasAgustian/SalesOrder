using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SalesOrder.Models
{
    public class Customer
    {
        public int COM_CUSTOMER_ID { get; set; }
        public string CUSTOMER_NAME { get; set; }

        public int SO_ORDER_ID { get; set; }

        public int SO_ORDER_ID_ORDER { get; set; }
        public string ORDER_NO { get; set; }
        public string ORDER_DATE { get; set; }
        public string ADDRESS { get; set; }
        public int SO_ITEM_ID { get; set; }
        public string ITEM_NAME { get; set; }
        public int QUANTITY { get; set; }
        public int PRICE { get; set; }
    }
}