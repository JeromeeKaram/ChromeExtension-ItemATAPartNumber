using HtmlAgilityPack;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EIPD_WindowsApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private async void btnRun_Click(object sender, EventArgs e)
        {
            try
            {

                progressBar1.Style = ProgressBarStyle.Marquee;
                progressBar1.MarqueeAnimationSpeed = 30;
                progressBar1.Visible = true;

                var fileNamesEms = new List<Test>();

                var engineManualLink = txtEngineManual.Text;
                var eipdLink = txtEIPDLink.Text;

                await Task.Run(async () =>
                {

                    var columnNames = new List<string>() { "SERIES", "DMC", "Title", "EIPD_Match", "EIPD_MatchTitle", "Attempt", "PartOfDMC", "Records", "WordsMatched" };

                    var excelInstance = ExcelUtility.CreateExcelWithColumns("D:\\test1213456.xlsx", columnNames, "Test");

                    var lstmodsEM = extract_task(engineManualLink);

                    var eipdDMCs = await FindDMCStringsAsync(txtEIPDLink.Text);

                    foreach (ModuleInfo mod in lstmodsEM)
                    {
                        foreach (TaskInfo task in mod.m_lstTasks)
                        {
                            //fileNamesEms.Add(task.m_sHtmlLink);
                            var engineManualItem = new Test();
                            engineManualItem.Series = mod.m_sTitle;
                            var pageHeader = GetEngineManualPageTitle(txtEngineManual.Text, task.m_sHtmlLink);
                            task.m_sTitle = pageHeader;
                            engineManualItem.Title = pageHeader;
                            engineManualItem.DMC = task.m_sHtmlLink;
                            engineManualItem.DMCLink = CreateLink(txtEngineManual.Text, task.m_sHtmlLink);
                            fileNamesEms.Add(engineManualItem);
                        }
                    }

                    foreach (var fileNamesEm in fileNamesEms)
                    {
                        var emDMCPage = fileNamesEm.DMC;
                        var emDMC = fileNamesEm.DMC.Split('.')[0];
                        var emDMCTitle = fileNamesEm.Title;

                        //if (emDMC != "PW1100G-B-73-11-03-03A-421A-D") continue;

                        var dmcVariants = GetDMCVariants(emDMC);
                        var (DMCT, attempt) = FindValidEIPDMatch(emDMC, dmcVariants, emDMCTitle, eipdDMCs);

                        if (DMCT != null)
                        {
                            fileNamesEm.EIPDMatch = DMCT.DMC;
                            fileNamesEm.EIPDMatchLink = CreateLink(txtEIPDLink.Text, DMCT?.DMC);
                            fileNamesEm.EIPDMatchTitle = DMCT.DMCTitle;

                            var parts = attempt.Split('|');

                            fileNamesEm.Attempt = parts[0];
                            fileNamesEm.PartOfDMC = parts[1];
                            fileNamesEm.Records = parts[2];
                            fileNamesEm.WordsMatch = parts[3];
                        }
                    }

                    ExcelUtility.SVCWriteOldSheet_EPPlus1(excelInstance, fileNamesEms, "Test");
                    excelInstance.Save();

                });

                MessageBox.Show("Finished Processing");

                // Open the Excel file
                Process.Start(new ProcessStartInfo("D:\\test1213456.xlsx")
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error",
        MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                progressBar1.Visible = false;
            }
        }

        private string CreateLink(string link, string page)
        {
            string folderUrl = link.Substring(0, link.LastIndexOf('/') + 1);
            return $"{folderUrl}{page}";
        }

        private (DMCT, string) FindValidEIPDMatch(string dmc, List<string> dmcVariants, string emDMCTitle, List<DMCT> eipdDMCs)
        {
            string validEIPDMatch = "";
            int attempt = 0;

            foreach (var variant in dmcVariants)
            {
                attempt = attempt + 1;

                var findAll = eipdDMCs.Where(d => d.DMC.Contains(variant)).ToList();

                if (!findAll.Any())
                {
                    continue;
                }

                if (findAll.Count == 1)
                {
                    //validEIPDMatch = findAll[0].DMC;
                    return (findAll[0], $"{attempt}|{variant}|1|0");
                }
                else
                {
                    var dmcTitleWords = emDMCTitle.ToLower().Split(new[] { ' ' }).Where(w => !string.IsNullOrWhiteSpace(w)).ToArray();

                    var bestMatch = findAll
                        .Select(item => new
                        {
                            Item = item,
                            MatchCount = item.DMCTitle
                                .ToLower()
                                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                .Count(word => dmcTitleWords.Contains(word))
                        })
                        .OrderByDescending(x => x.MatchCount)
                        .First();

                    if (bestMatch.MatchCount > 0)
                    {
                        return (bestMatch.Item, $"{attempt}|{variant}|{findAll.Count}|{bestMatch.MatchCount}");
                    }
                    else
                    {
                        continue;
                    }
                }
            }
            return (null, "-1");
        }

        private List<string> GetDMCVariants(string emDMC)
        {
            var result = new List<string>();

            // Split the DMC
            string[] parts = emDMC.Split('-');

            // Remove PW1100G, 941A and D
            string[] dmcParts = parts.Skip(1).Take(parts.Length - 3).ToArray();

            // B-72-21-00-02A
            result.Add(string.Join("-", dmcParts));

            // B-72-21-00-02
            dmcParts[dmcParts.Length - 1] =
                dmcParts[dmcParts.Length - 1].Substring(0, 2);
            result.Add(string.Join("-", dmcParts));

            // B-72-21-00-0
            dmcParts[dmcParts.Length - 1] =
                dmcParts[dmcParts.Length - 1].Substring(0, 1);
            result.Add(string.Join("-", dmcParts));

            // B-72-21-00
            dmcParts = dmcParts.Take(dmcParts.Length - 1).ToArray();
            result.Add(string.Join("-", dmcParts));

            // B-72-21-0
            dmcParts[dmcParts.Length - 1] =
                dmcParts[dmcParts.Length - 1].Substring(0, 1);
            result.Add(string.Join("-", dmcParts));

            // B-72-21
            dmcParts = dmcParts.Take(dmcParts.Length - 1).ToArray();
            result.Add(string.Join("-", dmcParts));

            return result;
        }

        public async Task<List<DMCT>> FindDMCStringsAsync(string url)
        {
            //var client = new HttpClient();
            //string html = await client.GetStringAsync(url);

            // Load HTML
            var web = new HtmlWeb();
            var doc = web.Load(url);

            // Find all nodes having data-dmc attribute
            var nodes = doc.DocumentNode.SelectNodes("//*[@data-dmc]");

            var dmcList = new List<DMCT>();

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    string dmc = node.GetAttributeValue("data-dmc", "");
                    string dmcTitle = node.GetAttributeValue("data-commontitle", "");

                    if (!string.IsNullOrWhiteSpace(dmc))
                    {
                        dmcList.Add(new DMCT { DMC = dmc, DMCTitle = dmcTitle });
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

        private string GetEngineManualPageTitle(string engineManualLink, string htmlPage)
        {
            string folderUrlEm = engineManualLink.Substring(0, engineManualLink.LastIndexOf('/') + 1); //http://127.0.0.1:8000/PW1000G-77445-19453-00/
            var partNumberPageURl = $"{folderUrlEm}{htmlPage}";

            var web = new HtmlWeb();
            var document = web.Load(partNumberPageURl);

            string pageHeader = document.DocumentNode.SelectSingleNode("//div[@class='header']/h1")?.InnerText?.Trim();

            return pageHeader;
        }

        private List<ModuleInfo> extract_task(string path)
        {
            List<ModuleInfo> lstmods = new List<ModuleInfo>();
            try
            {

                using (var client = new System.Net.WebClient())
                {
                    string contents = client.DownloadString(path);
                    string[] sArrHrefs = contents.Split(new string[1] { "<a href=\"#\">" }, StringSplitOptions.None);
                    for (int x = 1; x < sArrHrefs.Length; x++)
                    {
                        string href = sArrHrefs[x];
                        if (href.Contains("class=\"navDocLink\">"))
                        {
                            //Get the title
                            string title = SplitString(href, "<")[0];

                            //Get the Htmls
                            List<string> lstlinks = new List<string>();
                            List<string> lstSplits1 = SplitString(href, "class=\"navDocLink\">");
                            for (int k = 0; k < lstSplits1.Count; k++)
                            {
                                List<string> lstSplits2 = SplitString(lstSplits1[k], new string[2] { "href=", " " }, false);
                                foreach (string html in lstSplits2)
                                {
                                    string html1 = html.Replace("data-dmc=", "").Replace("\"", "").Trim();
                                    if (html1.EndsWith(".html") && lstlinks.Contains(html1) == false)
                                    {
                                        lstlinks.Add(html1);
                                    }
                                }
                            }
                            //Add
                            lstmods.Add(new ModuleInfo(title, lstlinks));
                        }
                    }
                }
            }
            catch (Exception ee)
            {
                //Utility.WriteErrorLog("", ee.Message, ee.StackTrace);
            }
            return lstmods;
        }

        List<string> SplitString(string sValue, string schar)
        {
            List<string> lst = new List<string>();
            try
            {
                string[] sArr = sValue.Split(new string[1] { schar }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string ss in sArr)
                {
                    if (ss.Trim().Length > 0)
                    {
                        lst.Add(ss.Trim());
                    }
                }
            }
            catch (Exception ee)
            {
                //Utility.WriteErrorLog(ee);
            }
            return lst;
        }

        List<string> SplitString(string sValue, string[] schars, bool Toupper)
        {
            List<string> lst = new List<string>();
            try
            {
                string[] sArr = sValue.Split(schars, StringSplitOptions.RemoveEmptyEntries);
                foreach (string ss in sArr)
                {
                    if (ss.Trim().Length > 0)
                    {
                        if (Toupper == false)
                            lst.Add(ss.Trim());
                        else
                            lst.Add(ss.ToUpper().Trim());
                    }
                }
            }
            catch (Exception ee)
            {
                //Utility.WriteErrorLog(ee);
            }
            return lst;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
#if DEBUG
            txtEIPDLink.Text = "http://127.0.0.1:8000/PW1000G-77445-19453-00/PW1000G-77445-16995-00.html";
            txtEngineManual.Text = "http://127.0.0.1:8000/PW1000G-77445-19453-00/PW1000G-77445-16992-00.html";
#endif
        }
    }
}
