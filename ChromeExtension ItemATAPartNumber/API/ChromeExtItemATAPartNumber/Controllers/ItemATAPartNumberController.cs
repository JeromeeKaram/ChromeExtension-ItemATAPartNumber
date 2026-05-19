using ChromeExtItemATAPartNumber.Models;
using HtmlAgilityPack;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Numeric;
using OfficeOpenXml.FormulaParsing.Excel.Functions.RefAndLookup;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static OfficeOpenXml.ExcelErrorValue;
using Match = System.Text.RegularExpressions.Match;

namespace ChromeExtItemATAPartNumber.Controllers
{
    public class ItemATAPartNumberController : Controller
    {
        // GET: ModuleID
        public ActionResult Index()
        {
            return View();
        }

        // GET: ModuleID/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ModuleID/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ModuleID/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: ModuleID/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ModuleID/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: ModuleID/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ModuleID/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        public async Task<JsonResult> ATAPNAsync(string pageUrl, string itemNumber)
        {
            try
            {
                //string url = url1;
                //string url = "http://127.0.0.1:8000/PW1000G-77445-19453-00/PW1100G-C-74-00-00-01A-421A-D.html";

                string folderUrl = pageUrl.Substring(0, pageUrl.LastIndexOf('/') + 1);
                //http://127.0.0.1:8000/PW1000G-77445-19453-00/

                // Get filename from URL
                string fileName = Path.GetFileName(new Uri(pageUrl).AbsolutePath);

                // Remove .html extension
                string dmc = Path.GetFileNameWithoutExtension(fileName);

                // Extract SubDMC
                // Example: PW1100G-C-74-00-00-01A-421A-D
                // Needed: 74-00-00-01

                string[] parts = dmc.Split('-');

                // Clean the 4th section (remove trailing letters)
                parts[5] = new string(parts[5].TakeWhile(char.IsDigit).ToArray());

                // Take required parts
                string subDmc = string.Join("-", parts.Skip(2).Take(4));

                Console.WriteLine("DMC = " + dmc);
                Console.WriteLine("SubDMC = " + subDmc);

                var eipdHtmlPage = "PW1000G-77445-16995-00.html";

                var eipdUrl = $"{folderUrl}/{eipdHtmlPage}";
                //var eipdUrl = "http://127.0.0.1:8000/PW1000G-77445-19453-00/PW1000G-77445-16995-00.html";
                var dmcStrings = await FindDMCStringsAsync(eipdUrl);

                var matches = dmcStrings.Where(x => x.Contains(subDmc)).ToList();

                var firstMatch = matches.FirstOrDefault();

                var partNumber = "";
                if (firstMatch != null)
                {
                    //var partNumberPageURl = "http://127.0.0.1:8000/PW1000G-77445-19453-00/PW1100G-B-73-21-64-01A-941A-D.html";
                    var partNumberPageURl = $"{folderUrl}{firstMatch}";
                    partNumber = await FindPartNumberAsync(partNumberPageURl, itemNumber);
                }
                var myRow = new MyRow();
                myRow.ItemNumber = itemNumber;
                myRow.PartNumber = partNumber;
                myRow.ATACode = subDmc; //TODO

                //var output = $"ATA-{subDmc},PN-{partNumber},itemNumber-{itemNumber}";

                return Json(new
                {
                    ata = myRow.ATACode,
                    pn = myRow.PartNumber,
                    itemNumber = myRow.ItemNumber
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        private async Task<string> FindPartNumberAsync(string pageUrl, string itemNumber)
        {
            HttpClient client = new HttpClient();
            string html = await client.GetStringAsync(pageUrl);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Select table by class (all 4 class names included)
            var table = doc.DocumentNode.SelectSingleNode(
                "//table[contains(@class,'ipcTable') and contains(@class,'rowHoverIncludeApplic') and contains(@class,'setFixedHeaderEnabled')]"
            );

            if (table != null)
            {
                var rows = table.SelectNodes(".//tr");

                if (rows != null)
                {
                    foreach (var row in rows)
                    {
                        // first column
                        var firstCell = row.SelectSingleNode("./td[1]/span[last()]");

                        if (firstCell != null)
                        {
                            string firstText = firstCell?.InnerText.Trim();

                            if (string.Equals(firstText.Trim(), itemNumber.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                // get second column
                                var thirdCell = row.SelectSingleNode("./td[contains(@class,'comPart')]//a");

                                var thirdCellText = thirdCell?.InnerText.Trim();

                                Console.WriteLine("MATCH FOUND: " + firstText);
                                Console.WriteLine("SECOND COLUMN: " + thirdCellText);

                                return thirdCellText; // ✅ return 2nd column value
                            }
                        }
                    }
                }
            }
            else
            {
                return "";
            }
            return "";
        }

        public async Task<List<string>> FindDMCStringsAsync(string url)
        {
            HttpClient client = new HttpClient();
            string html = await client.GetStringAsync(url);

            // Load HTML
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Find all nodes having data-dmc attribute
            var nodes = doc.DocumentNode.SelectNodes("//*[@data-dmc]");

            List<string> dmcList = new List<string>();

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    string dmc = node.GetAttributeValue("data-dmc", "");

                    if (!string.IsNullOrWhiteSpace(dmc))
                    {
                        dmcList.Add(dmc);
                    }
                }
            }

            // Print all values
            //foreach (var dmc in dmcList.Distinct())
            //{
            //    Console.WriteLine(dmc);
            //}

            // Implementation to find DMC strings in the given URL
            return dmcList;
        }
    }
}
