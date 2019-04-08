// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Microsoft.Rest;

using Covenant.API;
using Covenant.API.Models;

namespace Elite.Menu.Tasks
{
    public class MenuCommandAssemblyTaskSet : MenuCommand
    {
        public MenuCommandAssemblyTaskSet(CovenantAPI CovenantClient) : base(CovenantClient) { }
        public override void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                TaskMenuItem taskMenuItem = ((TaskMenuItem)menuItem);
                List<string> commands = UserInput.Split(" ").ToList();
                IList<GruntTaskOption> options = taskMenuItem.Task.Options;
                GruntTaskOption option = options.FirstOrDefault(O => O.Name.ToLower() == commands[1].ToLower());
                if (commands.Count() < 3 || commands.First().ToLower() != "set")
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                }
                else if (commands[1].ToLower() == "AssemblyPath".ToLower())
                {
                    string FileName = Path.Combine(Common.EliteDataFolder, commands[2]);
                    if (!File.Exists(FileName))
                    {
                        menuItem.PrintInvalidOptionError(UserInput);
                        EliteConsole.PrintFormattedErrorLine("File: \"" + FileName + "\" does not exist on the local system.");
                        return;
                    }
                    options.FirstOrDefault(O => O.Name == "EncodedAssembly").Value = Convert.ToBase64String(File.ReadAllBytes(FileName));
                    CovenantClient.ApiGrunttasksPut(taskMenuItem.Task);
                }
                else if (option == null)
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    EliteConsole.PrintFormattedErrorLine("Invalid Set option: \"" + commands[1] + "\"");
                }
                else
                {
                    option.Value = String.Join(" ", commands.GetRange(2, commands.Count() - 2));
                    CovenantClient.ApiGrunttasksPut(taskMenuItem.Task);
                }
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandAssemblyReflectTaskSet : MenuCommand
    {
        public MenuCommandAssemblyReflectTaskSet(CovenantAPI CovenantClient) : base(CovenantClient) { }
        public override void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                TaskMenuItem taskMenuItem = ((TaskMenuItem)menuItem);
                List<string> commands = UserInput.Split(" ").ToList();
                IList<GruntTaskOption> options = taskMenuItem.Task.Options;
                GruntTaskOption option = options.FirstOrDefault(O => O.Name.ToLower() == commands[1].ToLower());
                if (commands.Count() < 3 || commands.First().ToLower() != "set")
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                }
                else if (commands[1].ToLower() == "AssemblyPath".ToLower())
                {
                    string FileName = Path.Combine(Common.EliteDataFolder, commands[2]);
                    if (!File.Exists(FileName))
                    {
                        menuItem.PrintInvalidOptionError(UserInput);
                        EliteConsole.PrintFormattedErrorLine("File: \"" + FileName + "\" does not exist on the local system.");
                        return;
                    }
                    options.FirstOrDefault(O => O.Name == "EncodedAssembly").Value = Convert.ToBase64String(File.ReadAllBytes(FileName));
                    CovenantClient.ApiGrunttasksPut(taskMenuItem.Task);
                }
                else if (option == null)
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    EliteConsole.PrintFormattedErrorLine("Invalid Set option: \"" + commands[1] + "\"");
                }
                else
                {
                    option.Value = String.Join(" ", commands.GetRange(2, commands.Count() - 2));
                    CovenantClient.ApiGrunttasksPut(taskMenuItem.Task);
                }
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandUploadTaskSet : MenuCommand
    {
		public MenuCommandUploadTaskSet(CovenantAPI CovenantClient) : base(CovenantClient) { }
        public override void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                TaskMenuItem taskMenuItem = ((TaskMenuItem)menuItem);
                List<string> commands = UserInput.Split(" ").ToList();
                IList<GruntTaskOption> options = taskMenuItem.Task.Options;
                GruntTaskOption option = options.FirstOrDefault(O => O.Name.ToLower() == commands[1].ToLower());
                if (commands.Count() < 3 || commands.First().ToLower() != "set")
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                }
                else if (commands[1].ToLower() == "FilePath".ToLower())
                {
                    string FilePath = Path.Combine(Common.EliteDataFolder, commands[2]);
                    if (!File.Exists(FilePath))
                    {
                        menuItem.PrintInvalidOptionError(UserInput);
                        EliteConsole.PrintFormattedErrorLine("File: \"" + FilePath + "\" does not exist on the local system.");
                        return;
                    }
                    options.FirstOrDefault(O => O.Name == "FileContents").Value = Convert.ToBase64String(File.ReadAllBytes(FilePath));
                    options.FirstOrDefault(O => O.Name == "FileName").Value = Path.GetFileName(FilePath);
                    CovenantClient.ApiGrunttasksPut(taskMenuItem.Task);
                }
                else if (option == null)
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    EliteConsole.PrintFormattedErrorLine("Invalid Set option: \"" + commands[1] + "\"");
                }
                else
                {
                    option.Value = String.Join(" ", commands.GetRange(2, commands.Count() - 2));
                    CovenantClient.ApiGrunttasksPut(taskMenuItem.Task);
                }
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandShellCodeTaskSet : MenuCommand
    {
        public MenuCommandShellCodeTaskSet(CovenantAPI CovenantClient) : base(CovenantClient) { }
        public override void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                TaskMenuItem taskMenuItem = ((TaskMenuItem)menuItem);
                List<string> commands = UserInput.Split(" ").ToList();
                IList<GruntTaskOption> options = taskMenuItem.Task.Options;
                GruntTaskOption option = options.FirstOrDefault(O => O.Name.ToLower() == commands[1].ToLower());
                if (commands.Count() < 3 || commands.First().ToLower() != "set")
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                }
                else if (commands[1].ToLower() == "ShellcodeBinFilePath".ToLower())
                {
                    string FilePath = Path.Combine(Common.EliteDataFolder, commands[2]);
                    if (!File.Exists(FilePath))
                    {
                        menuItem.PrintInvalidOptionError(UserInput);
                        EliteConsole.PrintFormattedErrorLine("File: \"" + FilePath + "\" does not exist on the local system.");
                        return;
                    }
                    byte[] FileContents = File.ReadAllBytes(FilePath);
                    StringBuilder hex = new StringBuilder();
                    foreach (byte b in FileContents)
                    {
                        hex.AppendFormat("{0:x2}", b);
                    }
                    options.FirstOrDefault(O => O.Name == "Hex").Value = hex.ToString();
                    CovenantClient.ApiGrunttasksPut(taskMenuItem.Task);
                }
                else if (option == null)
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    EliteConsole.PrintFormattedErrorLine("Invalid Set option: \"" + commands[1] + "\"");
                }
                else
                {
                    option.Value = String.Join(" ", commands.GetRange(2, commands.Count() - 2));
                    CovenantClient.ApiGrunttasksPut(taskMenuItem.Task);
                }
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }
}
