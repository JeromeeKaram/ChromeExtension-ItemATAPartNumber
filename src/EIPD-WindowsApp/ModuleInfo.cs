using System;
using System.Collections.Generic;

public class ModuleInfo
{
    public string m_sTitle = "";
    public List<TaskInfo> m_lstTasks = new List<TaskInfo>();
    public ModuleInfo(string title, List<string> lsttasks)
    {
        m_sTitle = title;
        foreach (string s in lsttasks)
            m_lstTasks.Add(new TaskInfo(s));
    }
}

public class TaskInfo
{
    //S.NO title   DMC issue number issue date Any other change
    public string m_sTitle = "";
    public string m_sHtmlLink = "";
    public string m_sDMC = "";
    public string m_sIssueNumber = "";
    public string m_sIssueDate = "";
    public string m_sChange = "";
    public TaskInfo(string link)
    {
        m_sHtmlLink = link;
    }
}
