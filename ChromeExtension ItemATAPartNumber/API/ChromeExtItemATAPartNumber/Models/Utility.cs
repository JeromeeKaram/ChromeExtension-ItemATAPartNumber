using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;

namespace ChromeExtItemATAPartNumber.Models
{
    public class Utility
    {
        //static int cnt = 0;
        //public static void ErrorLog(string _message)
        //{
        //    string debugText = ConfigurationManager.AppSettings["DEBUG"].ToString();
        //    if (debugText == "Y")
        //    {
        //        StreamWriter objLogFile = null;
        //        FileStream fileStream = null;
        //        string Errorlogpath = Path.Combine(Path.GetTempPath(), "ChromeExtModuleID"  + ".txt");
        //        try
        //        {
        //            string stmp = "";
        //            var logFileInfo = new FileInfo(Errorlogpath);
        //            var logDirInfo = new DirectoryInfo(logFileInfo.DirectoryName);
        //            if (!logDirInfo.Exists) logDirInfo.Create();
        //            if (!logFileInfo.Exists)
        //            {
        //                fileStream = logFileInfo.Create();
        //            }
        //            else
        //            {
        //                fileStream = new FileStream(Errorlogpath, FileMode.Append);
        //            }
        //            objLogFile = new StreamWriter(fileStream);
        //            stmp = DateTime.Now.ToString() + ":" + _message;
        //            objLogFile.WriteLine(stmp);
        //            objLogFile.WriteLine("...............................");
        //            objLogFile.Close();
        //        }
        //        catch (Exception ex)
        //        {
        //            //objLogFile.Close();
        //            objLogFile = null;
        //        }
        //    }
        //}
    }
}