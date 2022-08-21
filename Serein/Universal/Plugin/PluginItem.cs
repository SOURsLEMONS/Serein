﻿using Jint;

namespace Serein.Plugin
{
    internal class PluginItem
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public Engine Engine { get; set; } = null;
    }
}