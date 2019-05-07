// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.IO;
using Net = System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Rest;

using Covenant.API;
using Covenant.API.Models;

namespace Elite.Menu.Listeners
{
    public class MenuCommandHTTPListenerShow : MenuCommand
    {
        public MenuCommandHTTPListenerShow()
        {
            this.Name = "Show";
            this.Description = "Show HTTPListener options";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            menuItem.Refresh();
            HttpListener HttpListener = ((HTTPListenerMenuItem)menuItem).HttpListener;
            HttpProfile profile = ((HTTPListenerMenuItem)menuItem).HttpProfile;
            string SSLCertPath = ((HTTPListenerMenuItem)menuItem).SSLCertPath;

            EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.Parameter, "HTTP Listener");
            menu.Rows.Add(new List<string> { "Name:", HttpListener.Name });
            menu.Rows.Add(new List<string> { "Description:", HttpListener.Description });
            menu.Rows.Add(new List<string> { "URL:", HttpListener.Url });
            menu.Rows.Add(new List<string> { "  ConnectAddress:", HttpListener.ConnectAddress });
            menu.Rows.Add(new List<string> { "  BindAddress:", HttpListener.BindAddress });
            menu.Rows.Add(new List<string> { "  BindPort:", HttpListener.BindPort.ToString() });
            menu.Rows.Add(new List<string> { "  UseSSL:", (HttpListener.UseSSL ?? default ) ? "True" : "False" });
            menu.Rows.Add(new List<string> { "SSLCertPath:", SSLCertPath });
            menu.Rows.Add(new List<string> { "SSLCertPassword:", HttpListener.SslCertificatePassword });
            menu.Rows.Add(new List<string> { "SSLCertHash:", HttpListener.SslCertHash });
            menu.Rows.Add(new List<string> { "HttpProfile:", profile.Name });
            menu.Print();
        }
    }

    public class MenuCommandHTTPListenerStart : MenuCommand
    {
		public MenuCommandHTTPListenerStart(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Start";
            this.Description = "Start the HTTP Listener";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override async void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                string[] commands = UserInput.Split(" ");
                if (commands.Length != 1 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                HttpListener HttpListener = ((HTTPListenerMenuItem)menuItem).HttpListener;
                if ((HttpListener.UseSSL ?? default) && (string.IsNullOrEmpty(HttpListener.SslCertHash) || string.IsNullOrEmpty(HttpListener.SslCertificate)))
                {
                    EliteConsole.PrintWarning("No SSLCertificate specified. Would you like to generate and use a self-signed certificate? [y/N] ");
                    string input = EliteConsole.Read();
                    if (input.StartsWith("y", StringComparison.OrdinalIgnoreCase))
                    {
                        X509Certificate2 certificate = Utilities.CreateSelfSignedCertificate(HttpListener.BindAddress);

                        string autopath = "httplistener-" + HttpListener.Id + "-certificate.pfx";
                        File.WriteAllBytes(
                            Path.Combine(Common.EliteDataFolder, autopath),
                            certificate.Export(X509ContentType.Pfx, HttpListener.SslCertificatePassword)
                        );
                        EliteConsole.PrintFormattedHighlightLine("Certificate written to: " + autopath);
                        EliteConsole.PrintFormattedWarningLine("(Be sure to disable certificate validation on Launchers/Grunts using this self-signed certificate)");
                        menuItem.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(menuItem, "Set SSLCertPath " + autopath);
                        menuItem.Refresh();
                        HttpListener = ((HTTPListenerMenuItem)menuItem).HttpListener;
                    }
                    else
                    {
                        EliteConsole.PrintFormattedErrorLine("Must specify an SSLCertfiicate to Start an HTTP Listener with SSL.");
                        return;
                    }
                }
                HttpListener.Status = ListenerStatus.Active;
                await this.CovenantClient.ApiListenersHttpPutAsync(HttpListener);

                ((HTTPListenerMenuItem)menuItem).RefreshHTTPTemplate();
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandHTTPListenerSet : MenuCommand
    {
        public MenuCommandHTTPListenerSet(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Set";
            this.Description = "Set HTTPListener option";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "Option",
                    Values = new List<MenuCommandParameterValue> {
                        new MenuCommandParameterValue { Value = "Name" },
                        new MenuCommandParameterValue {
                            Value = "URL",
                            NextValueSuggestions = new List<string> { "http://", "https://" }
                        },
                        new MenuCommandParameterValue {
                            Value = "ConnectAddress"
                        },
                        new MenuCommandParameterValue {
                            Value = "BindAddress"
                        },
                        new MenuCommandParameterValue {
                            Value = "BindPort",
                            NextValueSuggestions = new List<string> { "80", "443", "8080" }
                        },
                        new MenuCommandParameterValue { Value = "Dns" },
                        new MenuCommandParameterValue {
                            Value = "UseSSL",
                            NextValueSuggestions = new List<string> { "True", "False" }
                        },
                        new MenuCommandParameterValue { Value = "SSLCertPath" },
                        new MenuCommandParameterValue { Value = "SSLCertPassword" },
                        new MenuCommandParameterValue { Value = "HttpProfile" }
                    }
                },
                new MenuCommandParameter { Name = "Value" }
            };
        }

        public override async void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                HttpListener httpListener = ((HTTPListenerMenuItem)menuItem).HttpListener;
                string[] commands = UserInput.Split(" ");
                if (commands.Length != 3 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                if (this.Parameters.FirstOrDefault(P => P.Name == "Option").Values.Select(V => V.Value).Contains(commands[1], StringComparer.OrdinalIgnoreCase))
                {
                    if (commands[1].Equals("name", StringComparison.OrdinalIgnoreCase))
                    {
                        httpListener.Name = commands[2];
                    }
                    else if (commands[1].Equals("url", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            httpListener.Url = commands[2];
                            Uri uri = new Uri(httpListener.Url);
                            httpListener.UseSSL = uri.Scheme == "https";
                            httpListener.ConnectAddress = uri.Host;
                            httpListener.BindPort = uri.Port;
                        }
                        catch (Exception)
                        {
                            EliteConsole.PrintFormattedErrorLine("Specified URL: \"" + commands[2] + "\" is not a valid URI");
                            menuItem.PrintInvalidOptionError(UserInput);
                            return;
                        }
                    }
                    else if (commands[1].Equals("connectaddress", StringComparison.OrdinalIgnoreCase))
                    {
                        httpListener.ConnectAddress = commands[2];
                        string scheme = (httpListener.UseSSL ?? default) ? "https://" : "http://";
                        Uri uri = new Uri(scheme + httpListener.ConnectAddress + ":" + httpListener.BindPort);
                        httpListener.Url = uri.ToString();
                    }
                    else if (commands[1].Equals("bindaddress", StringComparison.OrdinalIgnoreCase))
                    {
                        httpListener.BindAddress = commands[2];
                    }
                    else if (commands[1].Equals("bindport", StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(commands[2], out int n);
                        httpListener.BindPort = n;
                        string scheme = (httpListener.UseSSL ?? default) ? "https://" : "http://";
                        Uri uri = new Uri(scheme + httpListener.ConnectAddress + ":" + httpListener.BindPort);
                        httpListener.Url = uri.ToString();
                    }
                    else if (commands[1].Equals("usessl", StringComparison.OrdinalIgnoreCase))
                    {
                        httpListener.UseSSL = commands[2].StartsWith("t", StringComparison.OrdinalIgnoreCase);
                        string scheme = (httpListener.UseSSL ?? default) ? "https://" : "http://";
                        Uri uri = new Uri(scheme + httpListener.ConnectAddress + ":" + httpListener.BindPort);
                        httpListener.Url = uri.ToString();
                    }
                    else if (commands[1].Equals("sslcertpath", StringComparison.OrdinalIgnoreCase))
                    {
                        string FileName = Path.Combine(Common.EliteDataFolder, commands[2]);
                        if (!File.Exists(FileName))
                        {
                            menuItem.PrintInvalidOptionError(UserInput);
                            EliteConsole.PrintFormattedErrorLine("File: \"" + FileName + "\" does not exist on the local system.");
                            return;
                        }
                        X509Certificate2 certificate = new X509Certificate2(FileName, httpListener.SslCertificatePassword);
                        httpListener.SslCertificate = Convert.ToBase64String(File.ReadAllBytes(FileName));
                        ((HTTPListenerMenuItem)menuItem).SSLCertPath = FileName;
                    }
                    else if (commands[1].Equals("sslcertpassword", StringComparison.OrdinalIgnoreCase))
                    {
                        httpListener.SslCertificatePassword = commands[2];
                    }
                    else if (commands[1].Equals("httpprofile", StringComparison.OrdinalIgnoreCase))
                    {
                        HttpProfile profile = ((HTTPListenerMenuItem)menuItem).HttpProfiles.FirstOrDefault(HP => HP.Name.Equals(commands[2], StringComparison.OrdinalIgnoreCase));
                        if (profile == null)
                        {
                            menuItem.PrintInvalidOptionError(UserInput);
                            EliteConsole.PrintFormattedErrorLine("HttpProfile: \"" + commands[2] + "\" does not exist.");
                            return;
                        }
                        httpListener.ProfileId = profile.Id;
                    }
                    await this.CovenantClient.ApiListenersHttpPutAsync(httpListener);
                    menuItem.Refresh();
                }
                else
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                }
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public sealed class HTTPListenerMenuItem : MenuItem
    {
        public ListenerType ListenerType { get; set; }
        public HttpListener HttpListener { get; set; }
        public HttpProfile HttpProfile { get; set; }
        public List<HttpProfile> HttpProfiles { get; set; }
        public string SSLCertPath { get; set; } = "";

		public HTTPListenerMenuItem(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.MenuTitle = "HTTP";
            this.AdditionalOptions.Add(new MenuCommandHTTPListenerShow());
            this.AdditionalOptions.Add(new MenuCommandHTTPListenerStart(this.CovenantClient));
            var setCommand = new MenuCommandHTTPListenerSet(this.CovenantClient);
            this.AdditionalOptions.Add(setCommand);
            this.AdditionalOptions.Add(new MenuCommandGenericUnset(setCommand.Parameters.FirstOrDefault(P => P.Name == "Option").Values));
        }

        public void RefreshHTTPTemplate()
        {
            try
            {
                this.HttpListener = this.CovenantClient.ApiListenersHttpPost(new HttpListener());
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }

		public override bool ValidateMenuParameters(string[] parameters, bool forwardEntrance = true)
		{
            this.RefreshHTTPTemplate();
            this.Refresh();
            return true;
		}

		public override void Refresh()
		{
            try
            {
                this.HttpListener = this.CovenantClient.ApiListenersHttpByIdGet(this.HttpListener.Id ?? default);
                this.ListenerType = this.CovenantClient.ApiListenersTypesGet().FirstOrDefault(LT => LT.Name == "HTTP");
                this.HttpProfiles = this.CovenantClient.ApiProfilesHttpGet().ToList();
                this.HttpProfile = this.HttpProfiles.FirstOrDefault(HP => this.HttpListener.ProfileId == HP.Id);

                this.MenuTitle = this.ListenerType.Name;
                this.MenuDescription = this.ListenerType.Description;

                List<string> profiles = this.CovenantClient.ApiProfilesHttpGet()
                        .Select(P => P.Name)
                        .ToList();

                this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "Set").Parameters
                    .FirstOrDefault(P => P.Name == "Option").Values
                        .FirstOrDefault(V => V.Value == "HttpProfile")
                        .NextValueSuggestions = profiles;

                this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "Set").Parameters
                    .FirstOrDefault(P => P.Name == "Option").Values
                        .FirstOrDefault(V => V.Value == "SSLCertPath")
                        .NextValueSuggestions = Utilities.GetFilesForPath(Common.EliteDataFolder);

                this.SetupMenuAutoComplete();
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }

        public override void PrintMenu()
        {
            this.AdditionalOptions.FirstOrDefault(O => O.Name == "Show").Command(this, "");
        }
	}
}
