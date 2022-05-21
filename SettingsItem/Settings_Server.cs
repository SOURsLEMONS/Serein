﻿namespace Serein
{
    class Settings_Server
    {
        public string Path { get; set; } = "";
        public bool EnableRestart { get; set; } = false;
        public bool EnableOutputCommand { get; set; } = true;
        public bool EnableLog { get; set; } = false;
        public int OutputStyle { get; set; } = 0;
        public string StopCommand { get; set; } = "stop";
        public bool AutoStop { get; set; } = true;
    }
}