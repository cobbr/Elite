// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.Linq;
using System.Collections.Generic;

namespace Elite
{
    public class EliteConsoleMenu
    {
        public enum EliteConsoleMenuType
        {
            Menu,
            Parameter,
            List
        }

        public EliteConsoleMenuType MenuType { get; set; } = EliteConsoleMenuType.Menu;
        public string Title { get; set; } = "";
        public List<string> Columns { get; set; } = new List<string>();
        public List<List<string>> Rows { get; set; } = new List<List<string>>();
        public bool PrintEndBuffer { get; set; } = true;
        public bool ShouldShortenFields { get; set; } = true;

        private static string Spacer { get; } = "     ";
        private static int MaxFieldLength { get; } = 80;
        private static string Elipsis { get; } = "...";

        public EliteConsoleMenu(EliteConsoleMenuType MenuType = EliteConsoleMenuType.Menu, string Title = "")
        {
            this.MenuType = MenuType;
            this.Title = Title;
        }

        public void Print()
        {
            // Ensure enough columns for given rows
            if (Rows.Count > 0)
            {
                while (Rows.Select(R => R.Count).Max() > Columns.Count)
                {
                    Columns.Add("");
                }
                // Shortens overly lengthy fields
                this.ShortenFields();
            }
            // Calculate max Column Lengths
            List<int> ColumnsMaxLengths = Columns.Select(C => 0).ToList();
            for (int i = 0; i < Rows.Count; i++)
            {
                for (int j = 0; j < Rows[i].Count; j++)
                {
                    ColumnsMaxLengths[j] = Math.Max(ColumnsMaxLengths[j], Rows[i][j].Length);
                }
            }
            bool empty = !(ColumnsMaxLengths.Max() > 0);
            for (int i = 0; i < ColumnsMaxLengths.Count; i++)
            {
                // Remove empty columns, if it is not a completely empty menu
                if (ColumnsMaxLengths[i] == 0 && !empty)
                {
                    Rows.ForEach(R => R.RemoveAt(i));
                    Columns.RemoveAt(i);
                    ColumnsMaxLengths.RemoveAt(i);
                    i--;
                }
                else
                {
                    // Column name is the max, if longer than all the column's fields
                    ColumnsMaxLengths[i] = Math.Max(ColumnsMaxLengths[i], Columns[i].Length);
                }
            }

            EliteConsole.PrintInfoLine();
            EliteConsole.PrintInfoLine();
            switch (this.MenuType)
            {
                case EliteConsoleMenuType.Menu:
                    PrintMenuType(ColumnsMaxLengths);
                    break;
                case EliteConsoleMenuType.Parameter:
                    PrintParameterType(ColumnsMaxLengths);
                    break;
                case EliteConsoleMenuType.List:
                    PrintListType(ColumnsMaxLengths);
                    break;
            }
            if (this.PrintEndBuffer)
            {
                EliteConsole.PrintInfoLine();
                EliteConsole.PrintInfoLine();
            }
        }

        private void PrintMenuType(List<int> ColumnsMaxLengths)
        {
            EliteConsole.PrintInfo(Spacer);
            EliteConsole.PrintHighlightLine(this.Title);
            EliteConsole.PrintInfo(Spacer);
            EliteConsole.PrintInfoLine(new String('=', ColumnsMaxLengths.Sum() + Columns.Count - 1));
            foreach (List<string> row in Rows)
            {
                EliteConsole.PrintInfo(Spacer);
                for (int i = 0; i < row.Count; i++)
                {
                    EliteConsole.PrintInfo(row[i]);
                    EliteConsole.PrintInfo(new String(' ', ColumnsMaxLengths[i] - row[i].Length + 1));
                }
                EliteConsole.PrintInfoLine();
            }
        }

        private void PrintParameterType(List<int> ColumnsMaxLengths)
        {
            EliteConsole.PrintInfo(Spacer);
            EliteConsole.PrintHighlightLine(this.Title);
            EliteConsole.PrintInfo(Spacer);
            EliteConsole.PrintInfoLine(new String('=', ColumnsMaxLengths.Sum() + Columns.Count - 1));
            foreach (List<string> row in Rows)
            {
                EliteConsole.PrintInfo(Spacer);
                for (int i = 0; i < row.Count; i++)
                {
                    EliteConsole.PrintInfo(row[i]);
                    EliteConsole.PrintInfo(new String(' ', ColumnsMaxLengths[i] - row[i].Length + 1));
                }
                EliteConsole.PrintInfoLine();
            }
        }

        private void PrintListType(List<int> ColumnsMaxLengths)
        {
            EliteConsole.PrintInfo(Spacer);
            for (int i = 0; i < Columns.Count; i++)
            {
                EliteConsole.PrintInfo(Columns[i]);
                EliteConsole.PrintInfo(new String(' ', ColumnsMaxLengths[i] - Columns[i].Length + 1));
            }
            EliteConsole.PrintInfoLine();
            EliteConsole.PrintInfo(Spacer);
            for (int i = 0; i < Columns.Count; i++)
            {
                EliteConsole.PrintInfo(new String('-', Columns[i].Length));
                EliteConsole.PrintInfo(new String(' ', ColumnsMaxLengths[i] - Columns[i].Length + 1));
            }
            EliteConsole.PrintInfoLine();
            foreach (List<string> row in Rows)
            {
                EliteConsole.PrintInfo(Spacer);
                for (int i = 0; i < row.Count; i++)
                {
                    EliteConsole.PrintInfo(row[i]);
                    EliteConsole.PrintInfo(new String(' ', ColumnsMaxLengths[i] - row[i].Length + 1));
                }
                EliteConsole.PrintInfoLine();
            }
        }

        private void ShortenFields()
        {
            for (int i = 0; i < Columns.Count; i++)
            {
                Columns[i] = Short(Columns[i]);
            }
            for (int i = 0; i < Rows.Count; i++)
            {
                for (int j = 0; j < Rows[i].Count; j++)
                {
                    Rows[i][j] = Short(Rows[i][j]);
                }
            }
        }

        private string Short(string toShorten = "")
        {
            if (toShorten == null) { toShorten = ""; }
            if (!this.ShouldShortenFields) { return toShorten; }
            if (toShorten.Length > MaxFieldLength)
            {
                return toShorten.Substring(0, MaxFieldLength) + Elipsis;
            }
            return toShorten;
        }
    }

    public static class EliteConsole
    {
        private static ConsoleColor InfoColor = ConsoleColor.Gray;
        private static ConsoleColor HighlightColor = ConsoleColor.Cyan;
        private static ConsoleColor WarningColor = ConsoleColor.Yellow;
        private static ConsoleColor ErrorColor = ConsoleColor.Red;

        private static string InfoLabel = "[+]";
        private static string HighlightLabel = "[*]";
        private static string WarningLabel = "[-]";
        private static string ErrorLabel = "[!]";
        private static readonly object _ConsoleLock = new object();

        private static void PrintColor(string ToPrint = "", ConsoleColor color = ConsoleColor.DarkGray)
        {
            lock (_ConsoleLock)
            {
                Console.ForegroundColor = color;
                Console.Write(ToPrint);
                Console.ResetColor();
            }
        }

        private static void PrintColorLine(string ToPrint = "", ConsoleColor color = ConsoleColor.DarkGray)
        {
            lock (_ConsoleLock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(ToPrint);
                Console.ResetColor();
            }
        }

        public static void PrintInfo(string ToPrint = "")
        {
            PrintColor(ToPrint, EliteConsole.InfoColor);
        }

        public static void PrintInfoLine(string ToPrint = "")
        {
            PrintColorLine(ToPrint, EliteConsole.InfoColor);
        }

        public static void PrintFormattedInfo(string ToPrint = "")
        {
            PrintColor(EliteConsole.InfoLabel + " " + ToPrint, EliteConsole.InfoColor);
        }

        public static void PrintFormattedInfoLine(string ToPrint = "")
        {
            PrintColorLine(EliteConsole.InfoLabel + " " + ToPrint, EliteConsole.InfoColor);
        }

        public static void PrintHighlight(string ToPrint = "")
        {
            PrintColor(ToPrint, EliteConsole.HighlightColor);
        }

        public static void PrintHighlightLine(string ToPrint = "")
        {
            PrintColorLine(ToPrint, EliteConsole.HighlightColor);
        }

        public static void PrintFormattedHighlight(string ToPrint = "")
        {
            PrintColor(EliteConsole.HighlightLabel + " " + ToPrint, EliteConsole.HighlightColor);
        }

        public static void PrintFormattedHighlightLine(string ToPrint = "")
        {
            PrintColorLine(EliteConsole.HighlightLabel + " " + ToPrint, EliteConsole.HighlightColor);
        }

        public static void PrintWarning(string ToPrint = "")
        {
            PrintColor(ToPrint, EliteConsole.WarningColor);
        }

        public static void PrintWarningLine(string ToPrint = "")
        {
            PrintColorLine(ToPrint, EliteConsole.WarningColor);
        }

        public static void PrintFormattedWarning(string ToPrint = "")
        {
            PrintColor(EliteConsole.WarningLabel + " " + ToPrint, EliteConsole.WarningColor);
        }

        public static void PrintFormattedWarningLine(string ToPrint = "")
        {
            PrintColorLine(EliteConsole.WarningLabel + " " + ToPrint, EliteConsole.WarningColor);
        }

        public static void PrintError(string ToPrint = "")
        {
            PrintColor(ToPrint, EliteConsole.ErrorColor);
        }

        public static void PrintErrorLine(string ToPrint = "")
        {
            PrintColorLine(ToPrint, EliteConsole.ErrorColor);
        }

        public static void PrintFormattedError(string ToPrint = "")
        {
            PrintColorLine(EliteConsole.ErrorLabel + " " + ToPrint, EliteConsole.ErrorColor);
        }

        public static void PrintFormattedErrorLine(string ToPrint = "")
        {
            PrintColorLine(EliteConsole.ErrorLabel + " " + ToPrint, EliteConsole.ErrorColor);
        }

        public static string Read()
        {
            return Read("");
        }

        public static string Read(string prompt)
        {
            string input = ReadLine.Read(prompt);
            if (input.Trim().Length > 0)
            {
                ReadLine.AddHistory(input);
            }
            return input;
        }
    }
}
