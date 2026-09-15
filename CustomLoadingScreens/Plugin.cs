using System.Threading;
using System.Threading.Tasks;
using CustomLoadingScreens.Windows;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using KamiToolKit;

namespace CustomLoadingScreens;

public sealed class Plugin : IAsyncDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    private static LoadingScreenManager LoadingScreenManager { get; set; } = null!;
    private static ConflictManager ConflictManager { get; set; } = null!;

    private const string CommandName = "/customloadingscreens";

    public Configuration Configuration { get; private set; } = null!;

    public readonly WindowSystem WindowSystem = new("CustomLoadingScreens");
    private ConfigWindow ConfigWindow { get; set; } = null!;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        PluginInterface.Create<Service>();

        await KamiToolKitLibrary.InitializeAsync(PluginInterface, "CustomLoadingScreens");
        
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        
        ConflictManager = new ConflictManager();
        await ConflictManager.LoadAsync(PluginInterface);
        
        LoadingScreenManager = new LoadingScreenManager();
        await LoadingScreenManager.LoadAsync(Configuration);

        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });
        
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
    }

    public async ValueTask DisposeAsync()
    {
        await LoadingScreenManager.DisposeAsync();
        
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();

        Service.CommandManager.RemoveHandler(CommandName);
        
        await KamiToolKitLibrary.DisposeAsync();
    }

    private void OnCommand(string command, string args)
    {
        ConfigWindow.Toggle();
    }
    
    public void ToggleConfigUi() => ConfigWindow.Toggle();
}
