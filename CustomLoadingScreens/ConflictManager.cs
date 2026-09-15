using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin;

namespace CustomLoadingScreens;

public class ConflictManager : IAsyncDisposable
{
    private IDalamudPluginInterface pluginInterface = null!;
    
    // will have to make this into an array at some point if there's ever more conflicting plugins
    private const string DisallowedPlugin = "Dalamud.LoadingImage";

    public async Task LoadAsync(IDalamudPluginInterface pi)
    {
        pluginInterface = pi;
        pluginInterface.ActivePluginsChanged += OnPluginsChanged;

        await Task.Run(() =>
        {
            var installed = pluginInterface.InstalledPlugins.Where(x => x.IsLoaded).Select(x => x.InternalName);
            CheckForConflicts(installed);
        });
    }

    private void OnPluginsChanged(IActivePluginsChangedEventArgs args)
    {
        if (args.Kind == PluginListInvalidationKind.Loaded)
        {
            CheckForConflicts(args.AffectedInternalNames);
        }
    }

    private void CheckForConflicts(IEnumerable<string> internalNames)
    {
        if (internalNames.Contains(DisallowedPlugin))
        {
            ShowWarning();
        }
    }
    
    public static void ShowWarning()
    {
        Service.NotificationManager.AddNotification(new Notification
        {
            Content = "Fancy Loading Screens is currently installed. " +
                      "Custom Loading Screens will not work as expected!",
            InitialDuration = TimeSpan.FromSeconds(15),
            Type = NotificationType.Warning,
            Icon = INotificationIcon.From(FontAwesomeIcon.ExclamationTriangle)
        });
    }

    public ValueTask DisposeAsync()
    {
        pluginInterface.ActivePluginsChanged -= OnPluginsChanged;
        return ValueTask.CompletedTask;
    }
}
