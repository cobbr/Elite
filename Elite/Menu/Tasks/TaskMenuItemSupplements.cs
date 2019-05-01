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
    public class MenuCommandAssemblyTaskSet : MenuCommandTaskSet
    {
        public MenuCommandAssemblyTaskSet(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            Name = "Set";
            Description = "Set AssemblyTask option";
            Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Option" },
                new MenuCommandParameter { Name = "Value" }
            };
        }

        public override async void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                List<string> commands = UserInput.Split(" ").ToList();
                if (commands.Count() < 3 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                GruntTask task = ((TaskMenuItem)menuItem).Task;
                GruntTaskOption option = task.Options.FirstOrDefault(O => O.Name.Equals(commands[1], StringComparison.OrdinalIgnoreCase));
                if (commands[1].Equals("LocalFilePath", StringComparison.OrdinalIgnoreCase))
                {
                    string FileName = Path.Combine(Common.EliteDataFolder, commands[2]);
                    if (!File.Exists(FileName))
                    {
                        menuItem.PrintInvalidOptionError(UserInput);
                        EliteConsole.PrintFormattedErrorLine("File: \"" + FileName + "\" does not exist on the local system.");
                        return;
                    }
                    task.Options.FirstOrDefault(O => O.Name == "EncodedAssembly").Value = Convert.ToBase64String(File.ReadAllBytes(FileName));
                    await CovenantClient.ApiGrunttasksPutAsync(task);
                }
                else if (option == null)
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    EliteConsole.PrintFormattedErrorLine("Invalid Set option: \"" + commands[1] + "\"");
                    return;
                }
                else
                {
                    option.Value = String.Join(" ", commands.GetRange(2, commands.Count() - 2));
                    await CovenantClient.ApiGrunttasksPutAsync(task);
                }
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandAssemblyReflectTaskSet : MenuCommandTaskSet
    {
        public MenuCommandAssemblyReflectTaskSet(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            Name = "Set";
            Description = "Set AssemblyReflectTask option";
            Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Option" },
                new MenuCommandParameter { Name = "Value" }
            };
        }
        public override async void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                List<string> commands = UserInput.Split(" ").ToList();
                if (commands.Count() < 3 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                GruntTask task = ((TaskMenuItem)menuItem).Task;
                GruntTaskOption option = task.Options.FirstOrDefault(O => O.Name.Equals(commands[1], StringComparison.OrdinalIgnoreCase));
                if (commands[1].Equals("LocalFilePath", StringComparison.OrdinalIgnoreCase))
                {
                    string FileName = Path.Combine(Common.EliteDataFolder, commands[2]);
                    if (!File.Exists(FileName))
                    {
                        menuItem.PrintInvalidOptionError(UserInput);
                        EliteConsole.PrintFormattedErrorLine("File: \"" + FileName + "\" does not exist on the local system.");
                        return;
                    }
                    task.Options.FirstOrDefault(O => O.Name == "EncodedAssembly").Value = Convert.ToBase64String(File.ReadAllBytes(FileName));
                    await CovenantClient.ApiGrunttasksPutAsync(task);
                }
                else if (option == null)
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    EliteConsole.PrintFormattedErrorLine("Invalid Set option: \"" + commands[1] + "\"");
                    return;
                }
                else
                {
                    option.Value = String.Join(" ", commands.GetRange(2, commands.Count() - 2));
                    await CovenantClient.ApiGrunttasksPutAsync(task);
                }
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandUploadTaskSet : MenuCommandTaskSet
    {
		public MenuCommandUploadTaskSet(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            Name = "Set";
            Description = "Set UploadTask option";
            Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Option" },
                new MenuCommandParameter { Name = "Value" }
            };
        }
        public override async void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                List<string> commands = UserInput.Split(" ").ToList();
                if (commands.Count() < 3 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                GruntTask task = ((TaskMenuItem)menuItem).Task;
                GruntTaskOption option = task.Options.FirstOrDefault(O => O.Name.Equals(commands[1], StringComparison.OrdinalIgnoreCase));
                if (commands[1].Equals("LocalFilePath", StringComparison.OrdinalIgnoreCase))
                {
                    string FilePath = Path.Combine(Common.EliteDataFolder, commands[2]);
                    if (!File.Exists(FilePath))
                    {
                        menuItem.PrintInvalidOptionError(UserInput);
                        EliteConsole.PrintFormattedErrorLine("File: \"" + FilePath + "\" does not exist on the local system.");
                        return;
                    }
                    task.Options.FirstOrDefault(O => O.Name == "FileContents").Value = Convert.ToBase64String(File.ReadAllBytes(FilePath));
                    task.Options.FirstOrDefault(O => O.Name == "FileName").Value = Path.GetFileName(FilePath);
                    await this.CovenantClient.ApiGrunttasksPutAsync(task);
                }
                else if (option == null)
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    EliteConsole.PrintFormattedErrorLine("Invalid Set option: \"" + commands[1] + "\"");
                    return;
                }
                else
                {
                    option.Value = String.Join(" ", commands.GetRange(2, commands.Count() - 2));
                    await this.CovenantClient.ApiGrunttasksPutAsync(task);
                }
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandShellCodeTaskSet : MenuCommandTaskSet
    {
        public MenuCommandShellCodeTaskSet(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            Name = "Set";
            Description = "Set ShellCodeTask option";
            Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Option" },
                new MenuCommandParameter { Name = "Value" }
            };
        }

        public override async void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                List<string> commands = UserInput.Split(" ").ToList();
                if (commands.Count() < 3 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                GruntTask task = ((TaskMenuItem)menuItem).Task;
                GruntTaskOption option = task.Options.FirstOrDefault(O => O.Name.Equals(commands[1], StringComparison.OrdinalIgnoreCase));
                if (commands[1].Equals("LocalFilePath", StringComparison.OrdinalIgnoreCase))
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
                    task.Options.FirstOrDefault(O => O.Name == "Hex").Value = hex.ToString();
                    await this.CovenantClient.ApiGrunttasksPutAsync(task);
                }
                else if (option == null)
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    EliteConsole.PrintFormattedErrorLine("Invalid Set option: \"" + commands[1] + "\"");
                    return;
                }
                else
                {
                    option.Value = String.Join(" ", commands.GetRange(2, commands.Count() - 2));
                    await this.CovenantClient.ApiGrunttasksPutAsync(task);
                }
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }
}
