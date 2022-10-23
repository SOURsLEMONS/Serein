﻿using Serein.Base;
using Serein.Items;
using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Serein
{
    internal static class Logger
    {
        public static int Type = 1;
        public static void Out(LogType Type, params object[] objects)
        {
            if (Program.Ui != null && !Program.Ui.Disposing)
            {
                string Line = string.Empty;
                foreach (var o in objects)
                {
                    if (o != null) { Line += o.ToString() + " "; }
                }
                Line = Line.TrimEnd();
                switch (Type)
                {
                    case LogType.Debug:
                        if (Global.Settings.Serein.Debug)
                        {
                            Program.Ui.Debug_Append($"{DateTime.Now:T} {Line}");
                            if (!Directory.Exists(Global.Path + "\\logs\\debug"))
                                Directory.CreateDirectory(Global.Path + "\\logs\\debug");
                            try
                            {
                                File.AppendAllText(
                                    Global.Path + $"\\logs\\debug\\{DateTime.Now:yyyy-MM-dd}.log",
                                    $"{DateTime.Now:T} {Line}" + "\n",
                                    Encoding.UTF8
                                    );
                            }
                            catch { }
                        }
                        break;
                    case LogType.Server_Output:
                        Program.Ui.PanelConsoleWebBrowser_Invoke(Line);
                        break;
                    case LogType.Server_Notice:
                        Program.Ui.PanelConsoleWebBrowser_Invoke("<span style=\"color:#4B738D;font-weight: bold;\">[Serein]</span>" + Log.EscapeLog(Line));
                        break;
                    case LogType.Server_Clear:
                        Program.Ui.PanelConsoleWebBrowser_Invoke("#clear");
                        break;
                    case LogType.Bot_Output:
                        Program.Ui.BotWebBrowser_Invoke(Line);
                        break;
                    case LogType.Bot_Notice:
                        Program.Ui.BotWebBrowser_Invoke("<span style=\"color:#4B738D;font-weight: bold;\">[Serein]</span>" + Log.EscapeLog(Line));
                        break;
                    case LogType.Bot_Receive:
                        Program.Ui.BotWebBrowser_Invoke("<span style=\"color:#239B56;font-weight: bold;\">[↓]</span>" + Log.EscapeLog(Line));
                        break;
                    case LogType.Bot_Send:
                        Program.Ui.BotWebBrowser_Invoke("<span style=\"color:#2874A6;font-weight: bold;\">[↑]</span>" + Log.EscapeLog(Line));
                        break;
                    case LogType.Bot_Error:
                        Program.Ui.BotWebBrowser_Invoke("<span style=\"color:#BA4A00;font-weight: bold;\">[×]</span>" + Log.EscapeLog(Line));
                        break;
                    case LogType.Bot_Clear:
                        Program.Ui.BotWebBrowser_Invoke("#clear");
                        break;
                    case LogType.Plugin_Info:
                        Program.Ui.SereinPluginsWebBrowser_Invoke(Log.EscapeLog(Line));
                        break;
                    case LogType.Plugin_Notice:
                        Program.Ui.SereinPluginsWebBrowser_Invoke("<span style=\"color:#4B738D;font-weight: bold;\">[Serein]</span>" + Log.EscapeLog(Line));
                        break;
                    case LogType.Plugin_Error:
                        Program.Ui.SereinPluginsWebBrowser_Invoke("<span style=\"color:#BA4A00;font-weight: bold;\">[×]</span>" + Log.EscapeLog(Line));
                        break;
                    case LogType.Plugin_Warn:
                        Program.Ui.SereinPluginsWebBrowser_Invoke("<span style=\"color:#9c8022;font-weight: bold;\">[!]</span>" + Log.EscapeLog(Line));
                        break;
                    case LogType.Plugin_Clear:
                        Program.Ui.SereinPluginsWebBrowser_Invoke("#clear");
                        break;
                }
            }
        }

        /// <summary>
        /// 显示具有指定文本、标题、按钮和图标的消息框。
        /// </summary>
        /// <param name="Text">要在消息框中显示的文本。</param>
        /// <param name="Caption">要在消息框的标题栏中显示的文本。</param>
        /// <param name="Buttons">MessageBoxButtons 值之一，可指定在消息框中显示哪些按钮。</param>
        /// <param name="Icon">MessageBoxIcon 值之一，它指定在消息框中显示哪个图标。</param>
        /// <returns>按下的按钮为OK或Yes</returns>
        public static bool MsgBox(string Text, string Caption, int Buttons, int Icon)
        {
            DialogResult Result = MessageBox.Show(Text, Caption, (MessageBoxButtons)Buttons, (MessageBoxIcon)Icon);
            return Result == DialogResult.OK || Result == DialogResult.Yes;
        }
    }
}
