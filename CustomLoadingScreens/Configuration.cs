using System;
using System.Collections.Generic;
using Dalamud.Configuration;

namespace CustomLoadingScreens;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public List<string> ImagePaths = [];

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
