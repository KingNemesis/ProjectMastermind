using UnityEngine;
using UnityEditor;

public static class CSV_Editor_Tool 
{
    [MenuItem("Project MMD Tools/ Add to Report %F1")]
    static void Dev_AppendToReport()
    {
        CSV_Manager.AppendToReport(
            new string[7]
            {
                "AGENT 64", //RANDOM STUFF
                "AGENT 72",
                "AGENT 80",
                "AGENT 90",
                "AGENT 12",
                "AGENT 67",
                "AGENT 94"
            });
        Debug.Log("<color=green>Report Updated Successfully</color>");
        EditorApplication.Beep();
    }
    [MenuItem("Project MMD Tools/ Reset the Report %F12")]
    static void Dev_ResetReport()
    {
        CSV_Manager.CreateReport();
        Debug.Log("<color=blue>Report has been reset.</color>");
        EditorApplication.Beep();
    }
    [MenuItem("Project MMD Tools/ TEST %F11")]
    static void Dev_Tests()
    {
        //CSV_Manager.ReadSpecificValue(2, 3);
        //CSV_Manager.AppendToReportSession(new string[7]
        //   {
        //      "AGENT 64", //RANDOM STUFF
        //      "AGENT 72",
        //      "AGENT 80",
        //     "AGENT 90",
        //   "AGENT 12",
        //     "AGENT 67",
        //     "AGENT 94"
        //  });
        //CSV_Manager.UpdateTotalStats(2, 3, 4, 5, 6);
        CSV_Manager.AppendToReportSession(new string[2] { "LOL", "XD" }, 3, 2, 0, 10, 7);
        Debug.Log("<color=red>Report has been tested.</color>");
        EditorApplication.Beep();
    }
}
