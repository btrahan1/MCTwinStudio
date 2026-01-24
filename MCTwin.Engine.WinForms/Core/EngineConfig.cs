using System;
using System.IO;

namespace MCTwinStudio.Core
{
    public static class EngineConfig
    {
        public static string RootDataPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory;

        public static string ActorsDir => Path.Combine(RootDataPath, "Actors");
        public static string PropsDir => Path.Combine(RootDataPath, "Props");
        public static string ScenesDir => Path.Combine(RootDataPath, "Scenes");
        public static string RendererDir => Path.Combine(RootDataPath, "Assets");

        public static void Initialize(string customRoot = null)
        {
            if (!string.IsNullOrEmpty(customRoot)) RootDataPath = customRoot;

            Directory.CreateDirectory(ActorsDir);
            Directory.CreateDirectory(PropsDir);
            Directory.CreateDirectory(ScenesDir);
        }
    }
}
