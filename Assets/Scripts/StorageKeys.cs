﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts
{
    public static class StorageKeys
    {
        private static string GetString(string name)
        {
            return "storagekeys_" + name;
        }

        public static string
            BOARD = GetString("board"),
            MUSIC_VOLUME = GetString("music_volume"),
            HIGH_SCORE = GetString("highScore"),
            LAST_SCORE = GetString("last_score")
            ;
    }
}
