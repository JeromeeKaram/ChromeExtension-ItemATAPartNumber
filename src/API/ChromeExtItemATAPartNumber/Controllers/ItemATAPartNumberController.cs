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
        public const string EIPD_PAGE = "PW1000G-77445-16995-00.html";

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
                //http://127.0.0.1:8000/PW1000G-77445-19453-00/PW1100G-B-72-21-00-02A-530A-B.html (Engine Manual)
                //http://127.0.0.1:8000/PW1000G-77445-19453-00/PW1100G-B-72-21-00-02A-941A-D.html (EIPD)

                string folderUrl = pageUrl.Substring(0, pageUrl.LastIndexOf('/') + 1); //http://127.0.0.1:8000/PW1000G-77445-19453-00/

                // Get filename from URL
                string fileName = Path.GetFileName(new Uri(pageUrl).AbsolutePath);

                // Remove .html extension
                string dmc = Path.GetFileNameWithoutExtension(fileName);

                var eipdUrl = $"{folderUrl}/{EIPD_PAGE}"; //var eipdUrl = "http://127.0.0.1:8000/PW1000G-77445-19453-00/PW1000G-77445-16995-00.html";

                var dmcStrings = await FindDMCStringsAsync(eipdUrl);
                
                var match = Regex.Match(dmc, @"([A-Za-z]-\d{2}-\d{2}-\d{2}-\d{2}[A-Za-z])");
                //input  = PW1100G-B-72-21-00-05A-941A-D
                //output = B-72-21-00-05A

                if (match.Success)
                {
                    string value = match.Groups[1].Value;

                    var validDMCs = GetSearchPatterns(value);
                    //validDMCs = B-72-21-00-05A, B-72-21-00-04A, B-72-21-00-06A, B-72-21-00

                    int eipdLinksCount = 0;
                    var partNumbers = new List<EAPD>();
                    var base64Image = "";
                    var mapped_em_eipd_link = false;

                    foreach (var validDMC in validDMCs)
                    {
                        var matches = dmcStrings.Where(x => x.Contains(validDMC)).ToList();

                        if (!matches.Any()) continue;

                        //mapped_em_eipd_link = true;

                        eipdLinksCount = matches.Count;

                        var firstMatch = matches.FirstOrDefault();

                        if (firstMatch != null)
                        {
                            var partNumberPageURl = $"{folderUrl}{firstMatch}";//var partNumberPageURl = "http://127.0.0.1:8000/PW1000G-77445-19453-00/PW1100G-B-73-21-64-01A-941A-D.html";
                            partNumbers = await FindPartNumbersByNameAsync(partNumberPageURl, itemNumber);
                            base64Image = await GetBase64ImageString(partNumberPageURl);
                        }

                        break; // Exit the loop after the first match
                    }

                    return Json(new
                    {
                        mapped_em_eipd_link,
                        partNumbers,
                        base64Image,
                        eipdLinksCount
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { error = "DMC pattern not found in the URL." }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public static List<string> GetSearchPatterns(string input)
        {
            var result = new List<string> { input };

            // Capture: prefix + number + suffix
            var match = Regex.Match(input, @"^(.*-)(\d+)([A-Za-z]+)$");

            if (match.Success)
            {
                string prefix = match.Groups[1].Value;
                int number = int.Parse(match.Groups[2].Value);
                string suffix = match.Groups[3].Value;

                // Previous
                if (number > 1)
                    result.Add($"{prefix}{number - 1:D2}{suffix}");

                // Next
                result.Add($"{prefix}{number + 1:D2}{suffix}");

                // Base value
                result.Add(prefix.TrimEnd('-'));
            }

            return result;
        }

        private async Task<string> GetBase64ImageString(string eipdUrl)
        {
            var client = new HttpClient();
            var html = await client.GetStringAsync(eipdUrl);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var allImages = doc.DocumentNode.SelectNodes("//img");
            var imagesUrls = new List<string>();

            foreach (var img in allImages ?? Enumerable.Empty<HtmlNode>())
            {
                var src = img.GetAttributeValue("src", "");
                imagesUrls.Add(src);
            }

            if (imagesUrls.Count > 1)
            {
                var imageUrl = new Uri(new Uri(eipdUrl), imagesUrls[1]).AbsoluteUri;

                var response = await client.GetAsync(imageUrl);

                response.EnsureSuccessStatusCode();

                var bytes = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType;

                //var bytes = await client.GetByteArrayAsync(imageUrl);
                //var contentType = response.Content.Headers.ContentType?.MediaType;
                return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";


            }

            return "";
        }

        private async Task<List<EAPD>> FindPartNumbersByNumberAsync(string pageUrl, string itemNumber)
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

                                    string partDescription4thText = "";

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

                                        // ✅ PUT IT HERE (before InnerHtml extraction)
                                        var indentureNode = clone.SelectSingleNode(".//span[contains(@class,'indenturePartCell')]");
                                        if (indentureNode != null)
                                        {
                                            indentureNode.Remove();
                                        }

                                        partDescription4thHtml = HtmlEntity.DeEntitize(clone.InnerHtml)
                                                                    .Replace("&nbsp;", " ")
                                                                    .Replace("\u00A0", " ")
                                                                    .Replace("Â", " ");
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

        private async Task<List<EAPD>> FindPartNumbersByNameAsync(string pageUrl, string itemName)
        {

            //Remove s||S from itemName if its at the last character in the word

            if (itemName.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                int index = Math.Max(itemName.LastIndexOf('s'), itemName.LastIndexOf('S'));
                itemName = itemName.Remove(index, 1);
            }

            //

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
                    // Find header row
                    var headerRow = rows.FirstOrDefault(r => r.SelectNodes("./th") != null);

                    int descriptionColumn = -1;
                    int quantityColumn = -1;

                    if (headerRow != null)
                    {
                        var headers = headerRow.SelectNodes("./th");

                        for (int i = 0; i < headers.Count; i++)
                        {
                            string headerText = HtmlEntity.DeEntitize(headers[i].InnerText).Trim();

                            if (headerText.Equals("Description", StringComparison.OrdinalIgnoreCase))
                                descriptionColumn = i + 1;          // XPath is 1-based

                            if (headerText.Equals("UPA", StringComparison.OrdinalIgnoreCase))
                                quantityColumn = i + 1;          // XPath is 1-based
                        }
                    }

                    foreach (var row in rows.Where(r => r.SelectNodes("./td") != null))
                    {
                        // 3rd column
                        var firstCell = row.SelectSingleNode("./td[1]/span[last()]");
                        //var secondCell = row.SelectSingleNode("./td[2]/span[last()]");
                        var thirdCell = row.SelectSingleNode($"./td[{descriptionColumn}]/span[last()]");

                        var thirdCellAll = row.SelectSingleNode($"./td[{descriptionColumn}]");

                        if (thirdCell != null)
                        {
                            string thirdCellText = thirdCellAll?.InnerText.Trim();

                            if (!string.IsNullOrWhiteSpace(thirdCellText))
                            {
                                // Matches:
                                // 10A, 10B, 10C, 10 AA, 10CC, etc.
                                //string pattern = $"^{Regex.Escape(itemName.Trim())}\\s*[A-Za-z]+$";

                                if (thirdCellText.IndexOf(itemName, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    // third column - part number
                                    var partNumber3rdCell = row.SelectSingleNode("./td[contains(@class,'comPart')]//a");
                                    var partNumber3rdCellText = partNumber3rdCell?.InnerText.Trim();
                                    partNumber3rdCellText = partNumber3rdCellText.Replace("â€¢", "").Replace("Â", "");

                                    string partDescription4thText = "";

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

                                        // ✅ PUT IT HERE (before InnerHtml extraction)
                                        var indentureNode = clone.SelectSingleNode(".//span[contains(@class,'indenturePartCell')]");
                                        if (indentureNode != null)
                                        {
                                            indentureNode.Remove();
                                        }

                                        partDescription4thHtml = HtmlEntity.DeEntitize(clone.InnerHtml)
                                                                    .Replace("&nbsp;", " ")
                                                                    .Replace("\u00A0", " ")
                                                                    .Replace("Â", " ");
                                    }

                                    var partQuantity5thCell = row.SelectSingleNode($"./td[{quantityColumn}]/span[last()]");

                                    Console.WriteLine("MATCH FOUND: " + thirdCellText);
                                    Console.WriteLine("SECOND COLUMN: " + partNumber3rdCellText);

                                    var eapd = new EAPD();
                                    eapd.ATACode = "";
                                    eapd.PartNumber = partNumber3rdCellText;
                                    eapd.ItemNumber = firstCell?.InnerText.Trim();
                                    eapd.PartDescription = partDescription4thHtml;
                                    eapd.Quantity = partQuantity5thCell.InnerText.Trim();
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
