﻿using BloatyNosy;
using System;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;

namespace Features.Feature.Apps
{
    internal class StoreAppsPrivate : FeatureBase
    {
        private static readonly ErrorHelper logger = ErrorHelper.Instance;
        private readonly PowerShell powerShell = PowerShell.Create();

        public override string ID()
        {
            return "*[LOW] Remove bloatware based on private database (Configure with a right-click)";
        }

        public override string Info()
        {
            return "Open the bloaty.txt file in the app directory of BloatyNosy to edit your database or right click on this feature";
        }

        private void RemoveApps(string str)
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = "powershell.exe";
            startInfo.Arguments = str;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }

        public override bool CheckFeature()
        {
            try
            {
                string bloatyFilePath = Path.Combine(HelperTool.Utils.Data.DataRootDir, "bloaty.txt");
                if (!File.Exists(bloatyFilePath))
                {
                    logger.Log("Your private signature is free of bloatware.");
                    return false; // Indicate failure
                }

                string[] num = File.ReadAllLines(bloatyFilePath);

                using (PowerShell powerShell = PowerShell.Create())
                {
                    powerShell.AddCommand("get-appxpackage")
                        .AddCommand("Select").AddParameter("property", "name");

                    bool foundMatch = false;
                    logger.Log("The following apps would be removed based on your private bloatware database:");
                    foreach (string line in num)
                    {
                        string[] package = line.Split(':');
                        string appx = package[0].Trim();

                        foreach (PSObject result in powerShell.Invoke())
                        {
                            string current = result.ToString(); // Get the current app

                            if (current.Contains(appx))
                            {
                                foundMatch = true;
                                logger.Log("[-] " + appx);
                                break;
                            }
                        }
                    }
                    if (!foundMatch)
                    {
                        logger.Log("Your private scan is free of bloatware.\n[TIP] You can manually expand and maintain your private bloatware database \"bloaty.txt\" in \"app\" directory.");
                    }

                    return foundMatch; // Return value of foundMatch
                }
            }
            catch (Exception ex)
            {
                logger.Log("[!] An error occurred: " + ex.Message);
                return false; // Indicate failure
            }
        }

        public override bool DoFeature()
        {
            string[] num = File.ReadAllLines(HelperTool.Utils.Data.DataRootDir + "/bloaty.txt");
            powerShell.Commands.Clear();
            powerShell.AddCommand("get-appxpackage");
            powerShell.AddCommand("Select").AddParameter("property", "name");

            foreach (PSObject result in powerShell.Invoke())
            {
                string current = result.ToString(); // Get the current app

                for (int i = 0; i < num.Length; i++)
                {
                    string[] package = num[i].Split(':');
                    string appx = package[0];
                    string command = package[1];
                    try
                    {
                        if (current.Contains(appx))
                        {
                            logger.Log("[?] Removing: " + appx + " (Wait...)");
                            RemoveApps(command);
                            logger.Log("[-] Removed: " + appx);
                        }
                    }
                    catch (Exception ex)
                    { logger.Log("Error removing " + ex); }
                }
            }
            return true;
        }

        public override bool UndoFeature()
        {
            logger.Log("- [Remove Store Apps] This feature does not provide a restore mode.");
            return false;
        }
    }
}