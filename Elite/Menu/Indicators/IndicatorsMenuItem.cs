// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Rest;

using Covenant.API;
using Covenant.API.Models;

namespace Elite.Menu.Indicators
{
    public class MenuCommandIndicatorsShow : MenuCommand
    {
        public MenuCommandIndicatorsShow()
        {
            this.Name = "Show";
            this.Description = "Show Indicators";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            menuItem.Refresh();
            IndicatorsMenuItem indicatorsMenu = ((IndicatorsMenuItem)menuItem);
            EliteConsoleMenu targetIndicatorsMenu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.List, "Target Indicators");
            targetIndicatorsMenu.Columns.Add("Name");
            targetIndicatorsMenu.Columns.Add("ComputerName");
            targetIndicatorsMenu.Columns.Add("UserName");
            indicatorsMenu.TargetIndicators.ToList().ForEach(TI =>
            {
                targetIndicatorsMenu.Rows.Add(new List<string> {
                    TI.Name,
                    TI.ComputerName,
                    TI.UserName
                });
            });
            if (targetIndicatorsMenu.Rows.Count > 0)
            {
                targetIndicatorsMenu.PrintEndBuffer = false;
                targetIndicatorsMenu.Print();
            }

            EliteConsoleMenu networkIndicatorsMenu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.List, "Network Indicators");
            networkIndicatorsMenu.Columns.Add("Name");
            networkIndicatorsMenu.Columns.Add("Protocol");
            networkIndicatorsMenu.Columns.Add("Domain");
            networkIndicatorsMenu.Columns.Add("IPAddress");
            networkIndicatorsMenu.Columns.Add("Port");
            networkIndicatorsMenu.Columns.Add("URI");

            indicatorsMenu.NetworkIndicators.ToList().ForEach(NI =>
            {
                networkIndicatorsMenu.Rows.Add(new List<string> {
                    NI.Name,
                    NI.Protocol,
                    NI.Domain,
                    NI.IpAddress,
                    NI.Port.ToString(),
                    NI.Uri
                });
            });
            if (networkIndicatorsMenu.Rows.Count > 0)
            {
                networkIndicatorsMenu.PrintEndBuffer = false;
                networkIndicatorsMenu.Print();
            }

            EliteConsoleMenu fileIndicatorsMenu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.List, "File Indicators");
            fileIndicatorsMenu.Columns.Add("Name");
            fileIndicatorsMenu.Columns.Add("FileName");
            fileIndicatorsMenu.Columns.Add("FilePath");
            fileIndicatorsMenu.Columns.Add("SHA2");
            fileIndicatorsMenu.Columns.Add("SHA1");
            fileIndicatorsMenu.Columns.Add("MD5");
            indicatorsMenu.FileIndicators.ToList().ForEach(FI =>
            {
                fileIndicatorsMenu.Rows.Add(new List<string> {
                    FI.Name,
                    FI.FileName,
                    FI.FilePath,
                    FI.ShA2,
                    FI.ShA1,
                    FI.MD5
                });
            });
            if (fileIndicatorsMenu.Rows.Count > 0)
            {
                fileIndicatorsMenu.Print();
            }
        }
    }

    public sealed class IndicatorsMenuItem : MenuItem
    {
        public List<Indicator> AllIndicators { get; set; }
        public List<NetworkIndicator> NetworkIndicators { get; set; }
        public List<FileIndicator> FileIndicators { get; set; }
        public List<TargetIndicator> TargetIndicators { get; set; }

        public IndicatorsMenuItem(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.MenuTitle = "Indicators";
            this.MenuDescription = "Displays list of indicators.";

            this.AdditionalOptions.Add(new MenuCommandIndicatorsShow());
            this.Refresh();
        }

        public override bool ValidateMenuParameters(string[] parameters = null, bool forwardEntrance = true)
        {
            this.Refresh();
            return true;
        }

        public override void PrintMenu()
        {
            this.AdditionalOptions.FirstOrDefault(O => O.Name == "Show").Command(this, "");
        }

        public override void Refresh()
        {
            try
            {
                this.AllIndicators = this.CovenantClient.ApiIndicatorsGet().ToList();
                this.NetworkIndicators = this.CovenantClient.ApiIndicatorsNetworksGet().ToList();
                this.FileIndicators = this.CovenantClient.ApiIndicatorsFilesGet().ToList();
                this.TargetIndicators = this.CovenantClient.ApiIndicatorsTargetsGet().ToList();
                this.SetupMenuAutoComplete();
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }
}
