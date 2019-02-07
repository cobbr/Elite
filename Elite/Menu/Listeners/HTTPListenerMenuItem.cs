// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.IO;
using Net = System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

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
            HTTPListenerMenuItem httpListenerMenuItem = (HTTPListenerMenuItem)menuItem;
            httpListenerMenuItem.Refresh();
            EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.Parameter, "HTTP Listener");
            menu.Rows.Add(new List<string> { "Name:", httpListenerMenuItem.httpListener.Name });
            menu.Rows.Add(new List<string> { "Description:", httpListenerMenuItem.httpListener.Description });
            menu.Rows.Add(new List<string> { "URL:", httpListenerMenuItem.httpListener.Url });
            menu.Rows.Add(new List<string> { "  ConnectAddress:", httpListenerMenuItem.httpListener.ConnectAddress });
            menu.Rows.Add(new List<string> { "  BindAddress:", httpListenerMenuItem.httpListener.BindAddress });
            menu.Rows.Add(new List<string> { "  BindPort:", httpListenerMenuItem.httpListener.BindPort.ToString() });
            menu.Rows.Add(new List<string> { "  UseSSL:", (httpListenerMenuItem.httpListener.UseSSL ?? default ) ? "True" : "False" });
            menu.Rows.Add(new List<string> { "SSLCertPath:", httpListenerMenuItem.SSLCertPath });
            menu.Rows.Add(new List<string> { "SSLCertPassword:", httpListenerMenuItem.httpListener.SslCertificatePassword });
            menu.Rows.Add(new List<string> { "SSLCertHash:", httpListenerMenuItem.httpListener.SslCertHash });
            menu.Rows.Add(new List<string> { "HttpProfile:", httpListenerMenuItem.httpProfile.Name });
            menu.Print();
        }
    }

    public class MenuCommandHTTPListenerStart : MenuCommand
    {
		public MenuCommandHTTPListenerStart(CovenantAPI CovenantClient, EventPrinter EventPrinter) : base(CovenantClient, EventPrinter)
        {
            this.Name = "Start";
            this.Description = "Start the HTTP Listener";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            HTTPListenerMenuItem httpListenerMenuItem = (HTTPListenerMenuItem)menuItem;
            // TODO: error if http lsitener already on this port
            if ((httpListenerMenuItem.httpListener.UseSSL ?? default) && (httpListenerMenuItem.httpListener.SslCertHash == "" || httpListenerMenuItem.httpListener.SslCertificate == ""))
            {
                EliteConsole.PrintWarning("No SSLCertificate specified. Would you like to generate and use a self-signed certificate? [y/N] ");
                string input = EliteConsole.Read();
                if (input.ToLower().StartsWith("y"))
                {
                    X509Certificate2 certificate = Utilities.CreateSelfSignedCertificate(httpListenerMenuItem.httpListener.BindAddress);

                    string autopath = "httplistener-" + httpListenerMenuItem.httpListener.Id + "-certificate.pfx";
                    File.WriteAllBytes(Path.Combine(Common.EliteDataFolder, autopath),
                                       certificate.Export(X509ContentType.Pfx, httpListenerMenuItem.httpListener.SslCertificatePassword));
                    EliteConsole.PrintFormattedInfoLine("Certificate written to: " + autopath);
                    httpListenerMenuItem.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(httpListenerMenuItem, "Set SSLCertPath " + autopath);
                }
                else
                {
                    EliteConsole.PrintFormattedErrorLine("Must specify an SSLCertfiicate to Start an HTTP Listener with SSL.");
                    return;
                }
            }
            httpListenerMenuItem.Refresh();
            httpListenerMenuItem.httpListener.Status = ListenerStatus.Active;
            httpListenerMenuItem.httpListener = this.CovenantClient.ApiListenersHttpPut(httpListenerMenuItem.httpListener);

			EventModel eventModel = new EventModel {
				Message = "Started HTTP Listener: " + httpListenerMenuItem.httpListener.Name + " at: " + httpListenerMenuItem.httpListener.Url,
				Level = EventLevel.Highlight,
				Context = "*"
			};
			eventModel = this.CovenantClient.ApiEventsPost(eventModel);
			this.EventPrinter.PrintEvent(eventModel);
            httpListenerMenuItem.RefreshHTTPTemplate();
            httpListenerMenuItem.Refresh();
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
                            Value = "ConnectAddress",
                            NextValueSuggestions = Net.Dns.GetHostAddresses(Net.Dns.GetHostName()).Select(IP => IP.ToString()).ToList()
                        },
                        new MenuCommandParameterValue {
                            Value = "BindAddress",
                            NextValueSuggestions = Net.Dns.GetHostAddresses(Net.Dns.GetHostName()).Select(IP => IP.ToString()).ToList()
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
                        new MenuCommandParameterValue { Value = "SSLCertPath", NextValueSuggestions = Utilities.GetFilesForPath(Common.EliteDataFolder) },
                        new MenuCommandParameterValue { Value = "SSLCertPassword" },
                        new MenuCommandParameterValue { Value = "HttpProfile" }
                    }
                },
                new MenuCommandParameter { Name = "Value" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            HttpListener httpListener = ((HTTPListenerMenuItem)menuItem).httpListener;
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 3 || commands[0].ToLower() != "set")
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else if (this.Parameters.FirstOrDefault(P => P.Name == "Option").Values.Select(V => V.Value.ToLower()).Contains(commands[1].ToLower()))
            {
                if(commands[1].ToLower() == "name")
                {
                    httpListener.Name = commands[2];
                }
                else if (commands[1].ToLower() == "url")
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
                    }
                }
                else if (commands[1].ToLower() == "connectaddress")
                {
                    httpListener.ConnectAddress = commands[2];
                    string scheme = (httpListener.UseSSL ?? default) ? "https://" : "http://";
                    Uri uri = new Uri(scheme + httpListener.ConnectAddress + ":" + httpListener.BindPort);
                    httpListener.Url = uri.ToString();
                }
                else if (commands[1].ToLower() == "bindaddress")
                {
                    httpListener.BindAddress = commands[2];
                }
                else if (commands[1].ToLower() == "bindport")
                {
                    int.TryParse(commands[2], out int n);
                    httpListener.BindPort = n;
					string scheme = (httpListener.UseSSL ?? default) ? "https://" : "http://";
                    Uri uri = new Uri(scheme + httpListener.ConnectAddress + ":" + httpListener.BindPort);
                    httpListener.Url = uri.ToString();
                }
                else if (commands[1].ToLower() == "usessl")
                {
                    httpListener.UseSSL = commands[2].ToLower().StartsWith('t');
					string scheme = (httpListener.UseSSL ?? default) ? "https://" : "http://";
                    Uri uri = new Uri(scheme + httpListener.ConnectAddress + ":" + httpListener.BindPort);
                    httpListener.Url = uri.ToString();
                }
                else if (commands[1].ToLower() == "sslcertpath")
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
                else if (commands[1].ToLower() == "sslcertpassword")
                {
                    httpListener.SslCertificatePassword = commands[2];
                }
                else if (commands[1].ToLower() == "httpprofile")
                {
                    HttpProfile profile = this.CovenantClient.ApiProfilesHttpGet().FirstOrDefault(HP => HP.Name == commands[2]);
                    if (profile == null)
                    {
                        menuItem.PrintInvalidOptionError(UserInput);
                        EliteConsole.PrintFormattedErrorLine("HttpProfile: \"" + commands[2] + "\" does not exist.");
                        return;
                    }
                    httpListener.ProfileId = profile.Id;
                }
                this.CovenantClient.ApiListenersHttpPut(httpListener);
                menuItem.Refresh();
            }
            else
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
        }
    }

    public sealed class HTTPListenerMenuItem : MenuItem
    {
        public ListenerType listenerType { get; set; }
        public HttpListener httpListener { get; set; }
        public HttpProfile httpProfile { get; set; }
        public string SSLCertPath { get; set; } = "";

		public HTTPListenerMenuItem(CovenantAPI CovenantClient, EventPrinter EventPrinter) : base(CovenantClient, EventPrinter)
        {
            this.httpListener = this.CovenantClient.ApiListenersHttpPost(new HttpListener());
            this.httpProfile = this.CovenantClient.ApiListenersByIdProfileGet(this.httpListener.Id ?? default);
            this.listenerType = this.CovenantClient.ApiListenersTypesGet().FirstOrDefault(LT => LT.Name == "HTTP");
            this.MenuTitle = listenerType.Name;
            this.MenuDescription = listenerType.Description;

			this.AdditionalOptions.Add(new MenuCommandHTTPListenerShow());
			this.AdditionalOptions.Add(new MenuCommandHTTPListenerStart(this.CovenantClient, this.EventPrinter));
			var setCommand = new MenuCommandHTTPListenerSet(this.CovenantClient);
            this.AdditionalOptions.Add(setCommand);
            this.AdditionalOptions.Add(new MenuCommandGenericUnset(setCommand.Parameters.FirstOrDefault(P => P.Name == "Option").Values));

            this.Refresh();
        }

        public void RefreshHTTPTemplate()
        {
            this.httpListener = this.CovenantClient.ApiListenersHttpPost(new HttpListener());
        }

		public override bool ValidateMenuParameters(string[] parameters, bool forwardEntrance = true)
		{
            this.RefreshHTTPTemplate();
            this.Refresh();
            return true;
		}

		public override void Refresh()
		{
            this.httpListener = this.CovenantClient.ApiListenersHttpByIdGet(this.httpListener.Id ?? default);
            this.listenerType = this.CovenantClient.ApiListenersTypesGet().FirstOrDefault(LT => LT.Name == "HTTP");
            this.httpProfile = this.CovenantClient.ApiListenersByIdProfileGet(this.httpListener.Id ?? default);

            List<string> profiles = this.CovenantClient.ApiProfilesHttpGet()
                    .Select(P => P.Name)
                    .ToList();

            this.AdditionalOptions.FirstOrDefault(AO => AO.Name.ToLower() == "set").Parameters
                                  .FirstOrDefault(P => P.Name.ToLower() == "option").Values
                                  .FirstOrDefault(V => V.Value.ToLower() == "httpprofile").NextValueSuggestions = profiles;
            this.SetupMenuAutoComplete();
        }

        public override void PrintMenu()
        {
            this.AdditionalOptions.FirstOrDefault(O => O.Name == "Show").Command(this, "");
        }
	}
}
