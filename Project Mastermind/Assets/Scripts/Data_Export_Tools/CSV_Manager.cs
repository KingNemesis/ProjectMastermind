using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

public static class CSV_Manager 
{
    private static string reportDirectoryName = "Report";
    private static string reportFileName = "data_report.csv";
    private static string reportSeparator = ",";
    private static string[] reportHeaders = new string[7]
    {
        "Total Stats",
        "AI Plans Created",
        "AI Plans Completed",
        "AI Plans Interrupted",
        "Player Actions",
        "Combat Duration",
        "Combat Log"
    };
    private static string[] sessionReportHeaders = new string[2]
    { "Session #",
      "Version 0.3"
    }; 


    private static string timeStampHeader = "Time stamp";
    #region Interactions
    public static void AppendToReport(string[] strings)
    {
        VerifyDirectory();
        VerifyFile();
        using(StreamWriter sw = File.AppendText(GetFilePath()))
        {
            string finalString = "";
            for (int i = 0; i < strings.Length; i++)
            {
                if(finalString!= "")
                {
                    finalString += reportSeparator;
                }
                finalString += strings[i];
            }
            finalString += reportSeparator + GetTimeStamp();
            sw.WriteLine(finalString);
        }
    }
    public static void AppendToReportAgent(List<string> combatLog)
    {
        VerifyDirectory();
        VerifyFile();
        using (StreamWriter sw = File.AppendText(GetFilePath()))
        {
            string finalString = "";
            foreach(string info in combatLog)
            {
                if (finalString != "")
                {
                    finalString += reportSeparator;
                }
                finalString += info;
            }
            finalString += reportSeparator + GetTimeStamp();
            sw.WriteLine(finalString);
        }
    }
    public static void AppendToReportSession(string[] agentStrings, int newPlansCreated, int newPlansCompleted, 
        int newPlansInterrupted, int newPlayerActions, int newCombatDuration)
    {
        VerifyDirectory();
        VerifyFile();

        UpdateTotalStats(newPlansCreated, newPlansCompleted, newPlansInterrupted, newPlayerActions, newCombatDuration); //magic

        string versionID = "v0.3"; //Probably should get this from files.
        string sessionID = "Session #" + ReadSpecificValue(2,1) + " - " + versionID;
        
        using (StreamWriter sw = File.AppendText(GetFilePath()))
        {
            sw.WriteLine(sessionID);

            for (int i = 0; i < agentStrings.Length; i++)
            {
                if(agentStrings[i] != null)
                {
                    sw.WriteLine(agentStrings[i]);
                }
            }
        }
    }
    public static void UpdateTotalStats(int newPlansCreated, int newPlansCompleted, int newPlansInterrupted, 
        int newPlayerActions, int newCombatDuration)
    {
        int totalSessions = Int32.Parse(ReadSpecificValue(2, 1));
        int totalPlansCreated = Int32.Parse(ReadSpecificValue(2, 2)); //maybe tryParse w/e
        int totalPlansCompleted = Int32.Parse(ReadSpecificValue(2, 3));
        int totalPlansInterrupted = Int32.Parse(ReadSpecificValue(2, 4));
        int totalPlayerActions = Int32.Parse(ReadSpecificValue(2, 5));
        int totalCombatDuration = Int32.Parse(ReadSpecificValue(2, 6));

        totalSessions++;
        totalPlansCreated += newPlansCreated;
        totalPlansCompleted += newPlansCompleted;
        totalPlansInterrupted += newPlansInterrupted;
        totalPlayerActions += newPlayerActions;
        totalCombatDuration += newCombatDuration;

        string newTotalStats = totalSessions + reportSeparator + totalPlansCreated + reportSeparator + totalPlansCompleted
            + reportSeparator + totalPlansInterrupted + reportSeparator + totalPlayerActions + reportSeparator + totalCombatDuration;

        LineChanger(newTotalStats, 2);
    }
    public static void LineChanger(string newText, int line_to_edit)
    {
        string[] arrLine = File.ReadAllLines(GetFilePath());
        arrLine[line_to_edit - 1] = newText;
        File.WriteAllLines(GetFilePath(), arrLine);
    }
    public static string ReadSpecificValue(int line, int positionInLine)
    {
        VerifyDirectory();
        VerifyFile();

        string finalString = "";
        int numberOfCommas = 0;

        string lineContents = ReadSpecificLine(line);         //First we get the desired line...

        char[] brokenDownString = lineContents.ToCharArray(); //...then we break it down to chars

        foreach (char c in brokenDownString)                  //...and finally we get the value.
        {
            if (c != ',')
            {
                if (positionInLine == numberOfCommas + 1)
                {
                    finalString += c;
                }
            }
            else
            {
                numberOfCommas++;
            }
        }
        return finalString;
    }
    public static string ReadSpecificLine(int line)
    {
        VerifyDirectory();
        VerifyFile();
        
        string lineContent = "";

        using (StreamReader sr = new StreamReader(GetFilePath()))
        {
            for (int i = 0; i < line; i++)
            {
                lineContent = sr.ReadLine();
            }
        }
        return lineContent;
    }
    public static void CreateReport()
    {
        VerifyDirectory(); //Check if directory exists

        using(StreamWriter sw = File.CreateText(GetFilePath()))
        {
            string finalString = "";
            for (int i = 0; i < reportHeaders.Length; i++)
            {
                if (finalString != "")
                {
                    finalString += reportSeparator;
                }
                finalString += reportHeaders[i];
            }
            //finalString += reportSeparator + timeStampHeader; --- NOT NECESSARY IN THIS VERSION ---
            sw.WriteLine(finalString);
            sw.WriteLine("0,0,0,0,0,0"); //null second line
        }
    }
    #endregion
    #region Operations
    static void VerifyDirectory()
    {
        string dir = GetDirectoryPath();

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }
    static void VerifyFile()
    {
        string file = GetFilePath();
        if (!File.Exists(file))
        {
            CreateReport();
        }
    }
    #endregion
    #region Queries
    static string GetDirectoryPath()
    {    
        return Application.dataPath + "/" + reportDirectoryName;
    }
    static string GetFilePath()
    {
        return GetDirectoryPath() + "/" + reportFileName;
    }
    static string GetTimeStamp()
    {
        return System.DateTime.Now.ToString();
    }
    #endregion
}
