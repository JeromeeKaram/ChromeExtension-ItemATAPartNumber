using HtmlAgilityPack;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Windows.Documents;
using System.Xml.Linq;

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

                var subDmc = await GetSubDMCAsync(dmc);

                Console.WriteLine("DMC = " + dmc);
                Console.WriteLine("SubDMC = " + subDmc);

                var eipdHtmlPage = "PW1000G-77445-16995-00.html";

                var eipdUrl = $"{folderUrl}/{eipdHtmlPage}";
                //var eipdUrl = "http://127.0.0.1:8000/PW1000G-77445-19453-00/PW1000G-77445-16995-00.html";
                var dmcStrings = await FindDMCStringsAsync(eipdUrl);

                var matches = dmcStrings.Where(x => x.Contains(subDmc)).ToList();

                var firstMatch = matches.FirstOrDefault();

                var partNumbers = new List<EAPD>();
                if (firstMatch != null)
                {
                    //var partNumberPageURl = "http://127.0.0.1:8000/PW1000G-77445-19453-00/PW1100G-B-73-21-64-01A-941A-D.html";
                    var partNumberPageURl = $"{folderUrl}{firstMatch}";
                    partNumbers = await FindPartNumbersAsync(partNumberPageURl, itemNumber);
                }

                return Json(new
                {
                    partNumbers
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        private async Task<List<EAPD>> FindPartNumbersAsync(string pageUrl, string itemNumber)
        {
            HttpClient client = new HttpClient();
            string html = await client.GetStringAsync(pageUrl);
            var matchedPartNumbers = new List<EAPD>();
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
                            string itemNumberSelected = firstCell?.InnerText.Trim();

                            if (!string.IsNullOrWhiteSpace(itemNumberSelected))
                            {
                                // Matches:
                                // 10A, 10B, 10C, 10 AA, 10CC, etc.
                                string pattern = $"^{Regex.Escape(itemNumber.Trim())}\\s*[A-Za-z]+$";

                                if (Regex.IsMatch(itemNumberSelected, pattern, RegexOptions.IgnoreCase))
                                {
                                    // third column - part number
                                    var partNumber3rdCell = row.SelectSingleNode("./td[contains(@class,'comPart')]//a");
                                    var partNumber3rdCellText = partNumber3rdCell?.InnerText.Trim();
                                    partNumber3rdCellText = partNumber3rdCellText.Replace("â€¢", "").Replace("Â", "");

                                    // fourth column - full description cell
                                    //var partDescription4thCell = row.SelectSingleNode("./td[contains(@class,'comDesc')]");

                                    //var partDescription4thCell = row.SelectSingleNode("./td[contains(@class,'comDesc')]");

                                    string partDescription4thText = "";

                                    //if (partDescription4thCell != null)
                                    //{
                                    //    // Clone node so original HTML is not modified
                                    //    var clone = partDescription4thCell.Clone();

                                    //    // Remove hyperlink tags but keep text
                                    //    foreach (var link in clone.SelectNodes(".//a") ?? Enumerable.Empty<HtmlNode>())
                                    //    {
                                    //        link.ParentNode.ReplaceChild(
                                    //            HtmlTextNode.CreateNode(link.InnerText),
                                    //            link
                                    //        );
                                    //    }

                                    //    var html1 = partDescription4thCell.InnerHtml
                                    //        .Replace("<br>", "###NEWLINE###")
                                    //        .Replace("<br/>", "###NEWLINE###")
                                    //        .Replace("<br />", "###NEWLINE###")
                                    //        .Replace("&nbsp;", " ")
                                    //        .Replace("\u00A0", " ");

                                    //    partDescription4thText = HtmlEntity.DeEntitize(HtmlNode.CreateNode("<div>" + html1 + "</div>").InnerText);

                                    //    // Cleanup formatting
                                    //    partDescription4thText = partDescription4thText
                                    //        .Replace("POST ", "\nPOST ")
                                    //        .Replace("PRE ", "\nPRE ")
                                    //        .Trim();
                                    //}

                                    //var partDescription4thText = HtmlEntity.DeEntitize(partDescription4thCell?.InnerText.Trim());
                                    //partDescription4thText = partDescription4thText.Replace("â€¢", "").Replace("Â", "");

                                    //                                    var partDescription4thCell = row.SelectSingleNode("./td[contains(@class,'comDesc')]");

                                    //string partDescription4thText = "";

                                    //if (partDescription4thCell != null)
                                    //{
                                    //    var clone = partDescription4thCell.Clone();

                                    //    // Remove hyperlinks but keep their text
                                    //    foreach (var link in clone.SelectNodes(".//a") ?? Enumerable.Empty<HtmlNode>())
                                    //    {
                                    //        link.ParentNode.ReplaceChild(
                                    //            HtmlTextNode.CreateNode(link.InnerText),
                                    //            link
                                    //        );
                                    //    }

                                    //    // Convert BR tags into actual newline characters
                                    //    foreach (var br in clone.SelectNodes(".//br") ?? Enumerable.Empty<HtmlNode>())
                                    //    {
                                    //        br.ParentNode.ReplaceChild(
                                    //            HtmlTextNode.CreateNode("\n"),
                                    //            br
                                    //        );
                                    //    }

                                    //    partDescription4thText = HtmlEntity.DeEntitize(clone.InnerText)
                                    //        .Replace("\u00A0", " ")
                                    //        .Trim();
                                    //}

                                    var partDescription4thCell = row.SelectSingleNode("./td[contains(@class,'comDesc')]");

                                    string partDescription4thHtml = "";

                                    if (partDescription4thCell != null)
                                    {
                                        var clone = partDescription4thCell.Clone();

                                        // Remove hyperlinks but keep their text
                                        foreach (var link in clone.SelectNodes(".//a") ?? Enumerable.Empty<HtmlNode>())
                                        {
                                            link.ParentNode.ReplaceChild(
                                                HtmlTextNode.CreateNode(link.InnerText),
                                                link
                                            );
                                        }

                                        partDescription4thHtml = HtmlEntity.DeEntitize(clone.InnerHtml)
                                            .Replace("&nbsp;", " ")
                                            .Replace("\u00A0", " ");
                                    }

                                    Console.WriteLine("MATCH FOUND: " + itemNumberSelected);
                                    Console.WriteLine("SECOND COLUMN: " + partNumber3rdCellText);

                                    var eapd = new EAPD();
                                    eapd.ATACode = "";
                                    eapd.PartNumber = partNumber3rdCellText;
                                    eapd.ItemNumber = itemNumberSelected;
                                    eapd.PartDescription = partDescription4thHtml;
                                    matchedPartNumbers.Add(eapd);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                return matchedPartNumbers;
            }
            return matchedPartNumbers;
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

        public async Task<string> GetSubDMCAsync(string DMC)
        {
            //// Example: PW1100G-C-74-00-00-01A-421A-D
            //// Needed: C-74-00-00
            ///
            var parts = DMC.Split('-');

            // C + first 3 numeric blocks
            return $"{parts[1]}-{parts[2]}-{parts[3]}-{parts[4]}";
        }
    }
}
