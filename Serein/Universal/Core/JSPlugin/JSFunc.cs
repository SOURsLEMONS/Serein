﻿using Jint;
using Jint.Native;
using Jint.Native.Function;
using Jint.Runtime;
using Newtonsoft.Json;
using Serein.Base;
using Serein.Core.Generic;
using Serein.Extensions;
using Serein.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RegExp = System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Serein.Core.JSPlugin
{
    internal static class JSFunc
    {
        public static long CurrentID;

        /// <summary>
        /// 注册插件
        /// </summary>
        /// <param name="namespace">命名空间</param>
        /// <param name="name">名称</param>
        /// <param name="version">版本</param>
        /// <param name="author">作者</param>
        /// <param name="description">介绍</param>
        /// <returns>注册结果</returns>
        public static string Register(
            string @namespace,
            string name,
            string version,
            string author,
            string description
            )
        {
            if (@namespace == null || !JSPluginManager.PluginDict.TryGetValue(@namespace, out Plugin plugin))
            {
                throw new ArgumentException("无法找到对应的命名空间", nameof(@namespace));
            }
            plugin.DisplayedName = name;
            plugin.DisplayedVersion = version;
            plugin.DisplayedAuthor = author;
            plugin.DisplayedDescription = description;
            return @namespace;
        }

        /// <summary>
        /// 注册事件监听器
        /// </summary>
        /// <param name="namespace">命名空间</param>
        /// <param name="eventName">事件名称</param>
        /// <param name="callback">函数</param>
        /// <returns>注册结果</returns>
        public static bool SetListener(string @namespace, string eventName, JsValue callback)
        {
            Logger.Output(LogType.Debug, "Namespace:", @namespace, "EventName:", eventName);
            eventName = System.Text.RegularExpressions.Regex.Replace(eventName ?? string.Empty, "^on", string.Empty);
            if (!Enum.IsDefined(typeof(EventType), eventName))
            {
                throw new ArgumentException("未知的事件：" + eventName);
            }
            lock (JSPluginManager.PluginDict)
            {
                return
                    callback is FunctionInstance &&
                    JSPluginManager.PluginDict.ContainsKey(@namespace) &&
                    JSPluginManager.PluginDict[@namespace].SetListener((EventType)Enum.Parse(typeof(EventType), eventName), callback);
            }

        }

        /// <summary>
        /// 事件触发异步锁
        /// </summary>
        private static readonly object EventLock = new();

        /// <summary>
        /// 触发插件事件
        /// </summary>
        /// <param name="type">事件名称</param>
        /// <param name="args">参数</param>
        /// <returns>是否拦截</returns>
        public static bool Trigger(EventType type, params object[] args)
        {
            if (JSPluginManager.PluginDict.Count == 0)
            {
                return false;
            }
            bool interdicted = false;
            lock (EventLock)
            {
                Logger.Output(LogType.Debug, type);
                lock (JSPluginManager.PluginDict)
                {
                    List<Task<bool>> tasks = new();
                    CancellationTokenSource tokenSource = new();
                    foreach (Plugin plugin in JSPluginManager.PluginDict.Values)
                    {
                        if (!plugin.Available || !plugin.HasListenedOn(type))
                        {
                            continue;
                        }
                        tasks.Add(Task.Run(() => plugin.Trigger(type, tokenSource.Token, args)));
                    }
                    if (tasks.Count > 0)
                    {
                        Task.WaitAll(tasks.ToArray(), Global.Settings.Serein.Function.JSEventMaxWaitingTime);
                        tokenSource.Cancel();
                        tasks.Select((task) => task.IsCompleted && task.Result).ToList().ForEach((b) => interdicted = interdicted || b);
                        Global.Settings.Serein.Function.JSEventCoolingDownTime.ToSleep();
                    }
                }
            }
            return interdicted;
        }

        /// <summary>
        /// 设置定时器
        /// </summary>
        /// <param name="namespace">命名空间</param>
        /// <param name="callback">函数</param>
        /// <param name="interval">间隔</param>
        /// <param name="autoReset"自动重置></param>
        /// <returns>定时器哈希值</returns>
        public static JsValue SetTimer(string @namespace, JsValue callback, JsValue interval, bool autoReset)
        {
            if (@namespace == null && !JSPluginManager.PluginDict.ContainsKey(@namespace))
            {
                throw new ArgumentException(nameof(@namespace), "无法找到对应的命名空间");
            }
            if (callback is not FunctionInstance)
            {
                throw new ArgumentException(nameof(callback), "The \"callback\" argument must be of type function.");
            }
            long timerID = CurrentID;
            CurrentID++;
            Logger.Output(LogType.Debug, "Interval:", interval, "AutoReset:", autoReset, "ID:", timerID);
            System.Timers.Timer timer = new(interval?.AsNumber() ?? 1)
            {
                AutoReset = autoReset,
            };
            timer.Elapsed += (_, _) =>
            {
                try
                {
                    @namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
                    if (!JSPluginManager.PluginDict.ContainsKey(@namespace))
                    {
                        throw new ArgumentException(nameof(@namespace), "无法找到对应的命名空间");
                    }
                    if (JSPluginManager.PluginDict[@namespace].Engine == null)
                    {
                        timer.Stop();
                        timer.Dispose();
                        JSPluginManager.Timers.Remove(timerID);
                        return;
                    }
                    lock (JSPluginManager.PluginDict[@namespace].Engine)
                    {
                        JSPluginManager.PluginDict[@namespace].Engine.Invoke(callback);
                    }
                }
                catch (Exception e)
                {
                    Logger.Output(LogType.Plugin_Error, $"[{@namespace}]", $"触发定时器[ID:{timerID}]时出现异常：\n{e.ToFullMsg()}");
                    Logger.Output(LogType.Debug, $"触发定时器[ID:{timerID}]时出现异常：", e);
                }
                if (!autoReset)
                {
                    timer.Dispose();
                    JSPluginManager.Timers.Remove(timerID);
                }
            };
            timer.Start();
            JSPluginManager.Timers.Add(timerID, timer);
            return timerID;
        }

        /// <summary>
        /// 清除定时器
        /// </summary>
        /// <param name="ID">哈希值</param>
        /// <returns>清除结果</returns>
        private static bool ClearTimer(long ID)
        {
            if (JSPluginManager.Timers.ContainsKey(ID))
            {
                JSPluginManager.Timers[ID].Stop();
                JSPluginManager.Timers[ID].Dispose();
                return JSPluginManager.Timers.Remove(ID);
            }
            return false;
        }

        /// <summary>
        /// 清除定时器
        /// </summary>
        /// <param name="ID">定时器ID</param>
        /// <returns>清除结果</returns>
        public static bool ClearTimer(JsValue ID) => ClearTimer((long)ID.AsNumber());

        /// <summary>
        /// 清除所有定时器
        /// </summary>
        public static void ClearAllTimers()
        {
            foreach (long ID in JSPluginManager.Timers.Keys.ToArray())
            {
                ClearTimer(ID);
            }
        }

        /// <summary>
        /// 获取完整报错信息
        /// </summary>
        public static string ToFullMsg(this Exception e)
        {
            if (e.InnerException is JavaScriptException javaScriptException1)
            {
                return $"{javaScriptException1.Message}\n{javaScriptException1.JavaScriptStackTrace}";
            }
            if (e is JavaScriptException javaScriptException2)
            {
                return $"{javaScriptException2.Message}\n{javaScriptException2.JavaScriptStackTrace}";
            }
            return (e.InnerException ?? e).Message;
        }

        /// <summary>
        /// 添加正则
        /// </summary>
        /// <returns>结果</returns>
        public static bool AddRegex(string exp, int area, bool needAdmin, string command, string remark, long[] ignored)
        {
            if (exp.TestRegex() &&
                area <= 4 && area >= 0 &&
                Command.GetType(command) != CommandType.Invalid)
            {
                lock (Global.RegexList)
                {
                    Global.RegexList.Add(new()
                    {
                        Expression = exp,
                        Area = area,
                        IsAdmin = needAdmin,
                        Command = command,
                        Remark = remark ?? string.Empty,
                        Ignored = ignored ?? Array.Empty<long>()
                    });
                }
                IO.SaveRegex();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 修改正则
        /// </summary>
        /// <returns>结果</returns>
        public static bool EditRegex(int index, string exp, int area, bool needAdmin, string command, string remark, long[] ignored)
        {
            if (index < Global.RegexList.Count &&
                index >= 0 &&
                exp.TestRegex() &&
                area <= 4 && area >= 0 &&
                Command.GetType(command) != CommandType.Invalid)
            {
                lock (Global.RegexList)
                {
                    Regex selected = Global.RegexList[index];
                    Global.RegexList[index] = new()
                    {
                        Expression = exp ?? selected.Expression,
                        Area = area,
                        IsAdmin = needAdmin,
                        Command = command ?? selected.Command,
                        Remark = remark ?? selected.Remark,
                        Ignored = ignored ?? selected.Ignored
                    };
                }
                IO.SaveRegex();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 删除正则
        /// </summary>
        /// <returns>结果</returns>
        public static bool RemoveRegex(int index)
        {
            if (index < Global.RegexList.Count && index >= 0)
            {
                lock (Global.RegexList)
                {
                    Global.RegexList.RemoveAt(index);
                }
                IO.SaveRegex();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 设置命令变量
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="jsValue">值</param>
        /// <returns>设置结果</returns>
        public static bool SetVariable(string key, JsValue jsValue)
        {
            string value = jsValue?.ToString();
            if (string.IsNullOrEmpty(value) || !RegExp.Regex.IsMatch(key ?? string.Empty, @"\w+"))
            {
                return false;
            }
            if (JSPluginManager.CommandVariablesDict.ContainsKey(key))
            {
                lock (JSPluginManager.CommandVariablesDict)
                {
                    JSPluginManager.CommandVariablesDict[key] = value;
                }
            }
            else
            {
                JSPluginManager.CommandVariablesDict.Add(key, value);
            }
            return true;
        }

        /// <summary>
        /// 导出变量
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="jsValue">值</param>
        public static void Export(string key, JsValue jsValue)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException(nameof(key));
            }
            if (JSPluginManager.VariablesExportedDict.ContainsKey(key))
            {
                lock (JSPluginManager.VariablesExportedDict)
                {
                    JSPluginManager.VariablesExportedDict[key] = jsValue;
                }
            }
            else
            {
                JSPluginManager.VariablesExportedDict.Add(key, jsValue);
            }
        }

        /// <summary>
        /// 设置预加载配置
        /// </summary>
        public static void SetPreLoadConfig(
            string @namespace,
            JsValue assemblies,
            JsValue allowGetType,
            JsValue allowOperatorOverloading,
            JsValue allowSystemReflection,
            JsValue allowWrite,
            JsValue strict,
            JsValue stringCompilationAllowed)
        {
            if (string.IsNullOrEmpty(@namespace))
            {
                throw new ArgumentException(nameof(@namespace));
            }
            IO.CreateDirectory(Path.Combine("plugins", @namespace));
            File.WriteAllText(
                Path.Combine("plugins", @namespace, "PreLoadConfig.json"),
                JsonConvert.SerializeObject(new PreLoadConfig
                {
                    Assemblies =
                        assemblies?.AsArray().ToList().Select((jsValue) => jsValue.ToString()).ToArray() ?? Array.Empty<string>(),
                    AllowGetType =
                        allowGetType?.IsBoolean() == true ? allowGetType.AsBoolean() : false,
                    AllowOperatorOverloading =
                        allowOperatorOverloading?.IsBoolean() == true ? allowOperatorOverloading.AsBoolean() : true,
                    AllowSystemReflection =
                        allowSystemReflection?.IsBoolean() == true ? allowSystemReflection.AsBoolean() : false,
                    AllowWrite =
                        allowWrite?.IsBoolean() == true ? allowWrite.AsBoolean() : true,
                    Strict =
                        strict?.IsBoolean() == true ? strict.AsBoolean() : false,
                    StringCompilationAllowed =
                        stringCompilationAllowed?.IsBoolean() == true ? stringCompilationAllowed.AsBoolean() : true,
                }, Formatting.Indented));
            Logger.Output(LogType.Plugin_Warn, $"[{@namespace}]", "预加载配置已修改，需要重新加载以应用新配置");
        }

        /// <summary>
        /// 热重载文件
        /// </summary>
        /// <param name="namespace">命名空间</param>
        /// <param name="type">类型</param>
        /// <returns>加载结果</returns>
        public static bool ReloadFiles(string @namespace, string type)
        {
            try
            {
                IO.Reload(type);
                Logger.Output(LogType.Plugin_Warn, $"[{@namespace}]", $"重新加载了Serein的{(type ?? string.Empty).ToLowerInvariant()}文件");
                return true;
            }
            catch (Exception e)
            {
                if (!string.IsNullOrEmpty(@namespace))
                {
                    Logger.Output(LogType.Plugin_Error, "重新加载指定的文件失败", e.Message);
                    Logger.Output(LogType.Debug, type, e);
                }
                return false;
            }
        }

        /// <summary>
        /// 安全调用
        /// </summary>
        /// <param name="jsValue">函数对象</param>
        /// <param name="arguments">参数</param>
        /// <returns>调用结果</returns>
        public static JsValue SafeCall(JsValue jsValue, JsValue[] arguments)
        {
            if (jsValue is not FunctionInstance functionInstance)
            {
                return JsValue.Undefined;
            }
            if (Monitor.TryEnter(functionInstance.Engine, 1000))
            {
                lock (functionInstance.Engine)
                {
                    return functionInstance.Engine.Invoke(functionInstance, arguments);
                }
            }
            throw new MethodAccessException();
        }
    }
}
