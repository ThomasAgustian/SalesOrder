using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Web;
using System.Web.Mvc;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using FreeDataExports;
using SalesOrder.Models;

namespace SalesOrder.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["Testing"].ToString();

            List<Customer> customers = new List<Customer>();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT soo.SO_ORDER_ID, soo.ORDER_NO, soo.ORDER_DATE, comc.CUSTOMER_NAME FROM SO_ORDER as soo LEFT JOIN COM_CUSTOMER as comc ON comc.COM_CUSTOMER_ID = soo.COM_CUSTOMER_ID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        SqlDataReader reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            Customer customer = new Customer
                            {
                                SO_ORDER_ID = Convert.ToInt32(reader["SO_ORDER_ID"]),
                                ORDER_NO = reader["ORDER_NO"].ToString(),
                                ORDER_DATE = DateTime.Parse(reader["ORDER_DATE"].ToString()).ToString("dd/MM/yyyy"),
                                CUSTOMER_NAME = reader["CUSTOMER_NAME"].ToString()
                            };
                            customers.Add(customer);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "Error: " + ex.Message;
                }
            }

            return View(customers);
        }

        public ActionResult Create_view()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Save_add(FormCollection form)
        {
            string ORDER_NO = form["ORDER_NO"];
            string ORDER_DATE = form["ORDER_DATE"];
            string COM_CUSTOMER_ID = form["COM_CUSTOMER_ID"];
            string ADDRESS = form["ADDRESS"];

            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["Testing"].ToString();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                string insertOrderQuery = @"
            INSERT INTO SO_ORDER (ORDER_NO, ORDER_DATE, COM_CUSTOMER_ID, ADDRESS) 
            VALUES (@ORDER_NO, @ORDER_DATE, @COM_CUSTOMER_ID, @ADDRESS);
            SELECT SCOPE_IDENTITY();"; 
                SqlCommand cmd = new SqlCommand(insertOrderQuery, conn);
                cmd.Parameters.AddWithValue("@ORDER_NO", ORDER_NO);
                cmd.Parameters.AddWithValue("@ORDER_DATE", ORDER_DATE);
                cmd.Parameters.AddWithValue("@COM_CUSTOMER_ID", COM_CUSTOMER_ID);
                cmd.Parameters.AddWithValue("@ADDRESS", ADDRESS);

                int SO_ORDER_ID = Convert.ToInt32(cmd.ExecuteScalar());

                string[] ITEM_NAME = form.GetValues("ITEM_NAME[]");
                string[] QUANTITY = form.GetValues("QUANTITY[]");
                string[] PRICE = form.GetValues("PRICE[]");

                for (int i = 0; i < ITEM_NAME.Length; i++)
                {
                    string itemName = ITEM_NAME[i];

                    if (int.TryParse(QUANTITY[i], out int quantity) && decimal.TryParse(PRICE[i], out decimal price))
                    {
                        string insertItemQuery = @"
                    INSERT INTO SO_ITEM (SO_ORDER_ID, ITEM_NAME, QUANTITY, PRICE) 
                    VALUES (@SO_ORDER_ID, @ITEM_NAME, @QUANTITY, @PRICE);";
                        SqlCommand cmdItem = new SqlCommand(insertItemQuery, conn);
                        cmdItem.Parameters.AddWithValue("@SO_ORDER_ID", SO_ORDER_ID);
                        cmdItem.Parameters.AddWithValue("@ITEM_NAME", itemName);
                        cmdItem.Parameters.AddWithValue("@QUANTITY", quantity);
                        cmdItem.Parameters.AddWithValue("@PRICE", price);
                        cmdItem.ExecuteNonQuery();
                    }
                }

                TempData["SuccessMessage"] = "Data has been successfully inserted.";
                return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult Edit_view(int SO_ORDER_ID)
        {
            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["Testing"].ToString();

            List<Customer> soOrders = new List<Customer>();
            List<Customer> soItems = new List<Customer>();

            using (SqlConnection connection = new SqlConnection(connString))
            {
                string query1 = "SELECT comc.COM_CUSTOMER_ID, comc.CUSTOMER_NAME, soo.SO_ORDER_ID, soo.ORDER_NO, soo.ORDER_DATE, comc.CUSTOMER_NAME, soo.ADDRESS FROM SO_ORDER as soo LEFT JOIN COM_CUSTOMER as comc ON comc.COM_CUSTOMER_ID = soo.COM_CUSTOMER_ID WHERE SO_ORDER_ID = @SO_ORDER_ID";

                using (SqlCommand command = new SqlCommand(query1, connection))
                {
                    command.Parameters.AddWithValue("@SO_ORDER_ID", SO_ORDER_ID);

                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Customer order = new Customer
                            {
                                CUSTOMER_NAME = reader.GetString(reader.GetOrdinal("CUSTOMER_NAME")),
                                SO_ORDER_ID = (int)reader.GetInt64(reader.GetOrdinal("SO_ORDER_ID")),
                                ORDER_NO = reader.GetString(reader.GetOrdinal("ORDER_NO")),
                                ORDER_DATE = reader.GetDateTime(reader.GetOrdinal("ORDER_DATE")).ToString("yyyy-MM-dd"), 
                                COM_CUSTOMER_ID = reader.GetInt32(reader.GetOrdinal("COM_CUSTOMER_ID")),
                                ADDRESS = reader.GetString(reader.GetOrdinal("ADDRESS")),
                            };
                            soOrders.Add(order);
                        }
                    }
                }

                string query2 = "SELECT SO_ITEM_ID, SO_ORDER_ID, ITEM_NAME, QUANTITY, PRICE FROM SO_ITEM WHERE SO_ORDER_ID = @SO_ORDER_ID";

                using (SqlCommand command = new SqlCommand(query2, connection))
                {
                    command.Parameters.AddWithValue("@SO_ORDER_ID", SO_ORDER_ID);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Customer item = new Customer
                            {
                                SO_ITEM_ID = (int)reader.GetInt64(reader.GetOrdinal("SO_ITEM_ID")),
                                SO_ORDER_ID_ORDER = (int)reader.GetInt64(reader.GetOrdinal("SO_ORDER_ID")),
                                ITEM_NAME = reader.GetString(reader.GetOrdinal("ITEM_NAME")), 
                                QUANTITY = reader.GetInt32(reader.GetOrdinal("QUANTITY")), 
                                PRICE = (int)reader.GetDouble(reader.GetOrdinal("PRICE"))
                            };
                            soItems.Add(item);
                        }
                    }
                }

            }

            SOOrderViewModel viewModel = new SOOrderViewModel
            {
                SO_ORDER = soOrders,
                SO_ITEM = soItems
            };

            return View(viewModel);
        }

        public ActionResult add_edit(FormCollection form)
        {
            string SO_ORDER_ID = form["SO_ORDER_ID_add"];
            string ITEM_NAME = form["addItemName"];
            string QUANTITY = form["addQuantity"];
            string PRICE = form["addPrice"];
            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["Testing"].ToString();

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                string insertOrderQuery = "INSERT INTO SO_ITEM (SO_ORDER_ID, ITEM_NAME, QUANTITY, PRICE) VALUES (@SO_ORDER_ID, @ITEM_NAME, @QUANTITY, @PRICE)";
                SqlCommand cmd = new SqlCommand(insertOrderQuery, conn);

                cmd.Parameters.AddWithValue("@SO_ORDER_ID", SO_ORDER_ID);
                cmd.Parameters.AddWithValue("@ITEM_NAME", ITEM_NAME);
                cmd.Parameters.AddWithValue("@QUANTITY", QUANTITY);
                cmd.Parameters.AddWithValue("@PRICE", PRICE);
                cmd.ExecuteNonQuery();
            }
            TempData["SuccessMessage"] = "Data has been successfully inserted.";
            return RedirectToAction("Edit_view", "Home", new { SO_ORDER_ID = SO_ORDER_ID });
        }
        public ActionResult edit(FormCollection form)
        {
            string SO_ORDER_ID = form["SO_ORDER_ID_edit"];
            string SO_ITEM_ID = form["editId"];
            string ITEM_NAME = form["editItemName"];
            string QUANTITY = form["editQuantity"];
            string PRICE = form["editPrice"];
            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["Testing"].ToString();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                string updateItemQuery = "UPDATE SO_ITEM SET ITEM_NAME = @ITEM_NAME, QUANTITY = @QUANTITY, PRICE = @PRICE WHERE SO_ITEM_ID = @SO_ITEM_ID";
                SqlCommand cmd = new SqlCommand(updateItemQuery, conn);
                cmd.Parameters.AddWithValue("@SO_ITEM_ID", SO_ITEM_ID);
                cmd.Parameters.AddWithValue("@ITEM_NAME", ITEM_NAME);
                cmd.Parameters.AddWithValue("@QUANTITY", QUANTITY);
                cmd.Parameters.AddWithValue("@PRICE", PRICE);
                cmd.ExecuteNonQuery();
            }
            TempData["SuccessMessage"] = "Data has been successfully edited.";
            return RedirectToAction("Edit_view", "Home", new { SO_ORDER_ID = SO_ORDER_ID });
        }

        public ActionResult delete(FormCollection form)
        {
            string SO_ORDER_ID = form["SO_ORDER_ID_delete"];
            string SO_ITEM_ID = form["deleteId"];
            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["Testing"].ToString();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                string deleteItemQuery = "DELETE FROM SO_ITEM WHERE SO_ITEM_ID = @SO_ITEM_ID";
                SqlCommand cmd = new SqlCommand(deleteItemQuery, conn);
                cmd.Parameters.AddWithValue("@SO_ITEM_ID", SO_ITEM_ID);
                cmd.ExecuteNonQuery();
            }
            TempData["SuccessMessage"] = "Data has been successfully Deleted.";
            return RedirectToAction("Edit_view", "Home", new { SO_ORDER_ID = SO_ORDER_ID });
        }

        public ActionResult edit_order(FormCollection form)
        {
            string SO_ORDER_ID = form["id"];
            string ORDER_NO = form["ORDER_NO"];
            string ORDER_DATE = form["ORDER_DATE"];
            string COM_CUSTOMER_ID = form["COM_CUSTOMER_ID"];
            string ADDRESS = form["ADDRESS"];
            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["Testing"].ToString();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                string deleteItemQuery = "UPDATE SO_ORDER SET ORDER_NO = @ORDER_NO, ORDER_DATE = @ORDER_DATE, COM_CUSTOMER_ID = @COM_CUSTOMER_ID, ADDRESS = @ADDRESS WHERE SO_ORDER_ID = @SO_ORDER_ID ";
                SqlCommand cmd = new SqlCommand(deleteItemQuery, conn);
                cmd.Parameters.AddWithValue("@SO_ORDER_ID", SO_ORDER_ID);
                cmd.Parameters.AddWithValue("@ORDER_NO", ORDER_NO);
                cmd.Parameters.AddWithValue("@ORDER_DATE", ORDER_DATE);
                cmd.Parameters.AddWithValue("@COM_CUSTOMER_ID", COM_CUSTOMER_ID);
                cmd.Parameters.AddWithValue("@ADDRESS", ADDRESS);
                cmd.ExecuteNonQuery();
            }
            TempData["SuccessMessage"] = "Data has been successfully Edited.";
            return RedirectToAction("Edit_view", "Home", new { SO_ORDER_ID = SO_ORDER_ID });
        }

        public ActionResult delete_index(FormCollection form)
        {
            string SO_ORDER_ID = form["SO_ORDER_ID"];
            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["Testing"].ToString();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                string deleteOrderQuery = "DELETE FROM SO_ORDER WHERE SO_ORDER_ID = @SO_ORDER_ID  DELETE FROM SO_ITEM WHERE SO_ORDER_ID = @SO_ORDER_ID ";
                SqlCommand cmd = new SqlCommand(deleteOrderQuery, conn);
                cmd.Parameters.AddWithValue("@SO_ORDER_ID", SO_ORDER_ID);
                cmd.ExecuteNonQuery();
            }
            TempData["SuccessMessage"] = "Data has been successfully Deleted.";
            return RedirectToAction("Index", "Home");
        }

        public ActionResult Export()
        {
            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["Testing"].ToString();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = @"
        SELECT soo.SO_ORDER_ID, soo.ORDER_NO, soo.ORDER_DATE, comc.CUSTOMER_NAME 
        FROM SO_ORDER as soo 
        LEFT JOIN COM_CUSTOMER as comc ON comc.COM_CUSTOMER_ID = soo.COM_CUSTOMER_ID";

                SqlDataAdapter dataAdapter = new SqlDataAdapter(query, conn);
                DataTable dataTable = new DataTable();

                dataAdapter.Fill(dataTable);
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Orders");
                    worksheet.Cell(1, 1).InsertTable(dataTable);

                    using (var memoryStream = new MemoryStream())
                    {
                        workbook.SaveAs(memoryStream);
                        return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "OrdersExport.xlsx");
                    }
                }
            }
        }
    }
}
