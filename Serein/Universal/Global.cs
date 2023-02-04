#if !CONSOLE
using Ookii.Dialogs.Wpf;
using System.Diagnostics;
#endif
using Serein.Base;
using Serein.Extensions;
using Serein.Items;
using Serein.JSPlugin;
using Serein.Settings;
using Serein.Server;
using System;
using System.Collections.Generic;
using System.IO;

namespace Serein
{
    internal static class Global
    {
        /// <summary>
        /// 程序路径
        /// </summary>
        public static readonly string PATH = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// 版本号
        /// </summary>
        public const string VERSION = "v1.3.4";

        /// <summary>
        /// 类型
        /// </summary>
        public const string TYPE =
#if CONSOLE
            "console";
#elif WINFORM
            "winform";
#elif WPF
            "wpf";
#else
            "unknown";
#endif

        /// <summary>
        /// 正则项列表
        /// </summary>
        public static List<Regex> RegexList
        {
            get
            {
                return _regexList;
            }
            set
            {
                lock (_regexList)
                {
                    _regexList = value;
                }
            }
        }

        private static List<Regex> _regexList = new();

        /// <summary>
        /// 任务项列表
        /// </summary>
        public static List<Task> TaskList
        {
            get
            {
                return _taskList;
            }
            set
            {
                lock (_taskList)
                {
                    _taskList = value;
                    _taskList.ForEach((task) => task.Check());
                }
            }
        }

        private static List<Task> _taskList = new();

        /// <summary>
        /// 成员项字典
        /// </summary>
        public static Dictionary<long, Member> MemberDict = new();

        /// <summary>
        /// 设置项
        /// </summary>
        public static Category Settings = new();

        /// <summary>
        /// 首次开启
        /// </summary>
        public static bool FirstOpen;

        /// <summary>
        /// 编译信息
        /// </summary>
        public static readonly BuildInfo BuildInfo = new();

        /// <summary>
        /// 群组用户信息缓存
        /// </summary>
        public static Dictionary<long, Dictionary<long, Member>> GroupCache = new();

        /// <summary>
        /// 启动基础事件
        /// </summary>
        public static void OnStart()
        {
            CrashInterception.Init();
            Directory.SetCurrentDirectory(PATH);
#if !CONSOLE
            ResourcesManager.InitConsole();
#endif
            IO.ReadAll();
            TaskRunner.Start();
            Net.Init();
            System.Threading.Tasks.Task.Run(SystemInfo.Init);
#if !CONSOLE
            AppDomain.CurrentDomain.ProcessExit += (_, _) => IO.Timer.Stop();
#endif
            AppDomain.CurrentDomain.ProcessExit += (_, _) => IO.LazyTimer.Stop();
            AppDomain.CurrentDomain.ProcessExit += (_, _) => IO.Update();
        }

        /// <summary>
        /// 加载后事件
        /// </summary>
        public static void OnLoaded()
        {
            IO.StartSaving();
            System.Threading.Tasks.Task.Run(JSPluginManager.Load);
            if (FirstOpen)
            {
                ShowWelcomePage();
            }
            IList<string> args = Environment.GetCommandLineArgs();
            if (File.Exists("Updater.exe"))
            {
                File.Delete("Updater.exe");
            }
            System.Threading.Tasks.Task.Run(() =>
            {
                (Settings.Serein.AutoRun.Delay > 0 ? Settings.Serein.AutoRun.Delay : 0).ToSleep();
                if (Settings.Serein.AutoRun.ConnectWS || args.Contains("auto_connect"))
                {
                    System.Threading.Tasks.Task.Run(Websocket.Open);
                }
                if (Settings.Serein.AutoRun.StartServer || args.Contains("auto_start"))
                {
                    System.Threading.Tasks.Task.Run(ServerManager.Start);
                }
            });
        }

        /// <summary>
        /// 显示欢迎页面
        /// </summary>
        public static void ShowWelcomePage()
        {
#if !CONSOLE
            TaskDialog taskDialog = new()
            {
                Buttons = {
                        new TaskDialogButton(ButtonType.Ok)
                    },
                MainInstruction = "欢迎使用Serein！！",
                WindowTitle = "Serein",
                Content = "" +
                    "如果你是第一次使用Serein，那么一定要仔细阅读以下内容，相信这些会对你有所帮助(๑•̀ㅂ•́)و✧\n" +
                    "◦ 官网文档：<a href=\"https://serein.cc\">https://serein.cc</a>\n" +
                    "◦ GitHub仓库：<a href=\"https://github.com/Zaitonn/Serein\">https://github.com/Zaitonn/Serein</a>\n" +
                    "◦ 交流群：<a href=\"https://jq.qq.com/?_wv=1027&k=XNZqPSPv\">954829203</a>",
                Footer = "此面板已发布在<a href=\"https://www.minebbs.com/resources/serein.4169/\">Minebbs</a>上，欢迎支持~",
                FooterIcon = TaskDialogIcon.Information,
                EnableHyperlinks = true,
                ExpandedInformation = "此软件与Mojang Studio、网易、Microsoft没有从属关系.\n" +
                     "Serein is licensed under <a href=\"https://github.com/Zaitonn/Serein/blob/main/LICENSE\">GPL-v3.0</a>\n" +
                     "Copyright © 2022 <a href=\"https://github.com/Zaitonn\">Zaitonn</a>. All Rights Reserved.",
            };
            taskDialog.HyperlinkClicked += (_, e) => Process.Start(new ProcessStartInfo(e.Href) { UseShellExecute = true });
            taskDialog.ShowDialog();
#else       
            Logger.Output(LogType.Info,
                "欢迎使用Serein！！\n" +
                "如果你是第一次使用Serein，那么一定要仔细阅读以下内容，相信这些会对你有所帮助OwO\n" +
                "◦ 官网文档：\x1b[4m\x1b[36mhttps://serein.cc\x1b[0m\n" +
                "◦ GitHub仓库：\x1b[4m\x1b[36mhttps://github.com/Zaitonn/Serein\x1b[0m\n" +
                "◦ 交流群：\x1b[4m\x1b[36m954829203\x1b[0m\n" +
                "（控制台不支持超链接，你可以复制后到浏览器中打开）");
#endif
        }
    }
}