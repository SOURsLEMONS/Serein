﻿using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serein.Extensions;

namespace Serein.Settings
{
    [JsonObject(MemberSerialization.OptOut, NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    internal class Matches
    {
        private const string _Difficulty = "(PEACEFUL|EASY|NORMAL|HARD|DIFFICULT[^Y])";

        public string Difficulty = "(PEACEFUL|EASY|NORMAL|HARD|DIFFICULT[^Y])";

        private const string _LevelName = @"Level\sName:\s(.+?)$";

        public string LevelName = @"Level\sName:\s(.+?)$";

        public string[] MuiltLines =
        {
            @"players\sonline:",
            "个玩家在线"
        };

        public Matches()
        {
            Difficulty = Difficulty.TestRegex() ? Difficulty : _Difficulty;
            LevelName = LevelName.TestRegex() ? LevelName : _LevelName;
        }
    }
}
