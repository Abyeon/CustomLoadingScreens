using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace CustomLoadingScreens.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    
    private FileDialogManager fileDialogManager;
    
    public ConfigWindow(Plugin plugin) : base("Custom Loading Screens###CustomLoadingScreensConfig")
    {
        Size = new Vector2(600, 600);

        configuration = plugin.Configuration;
        
        fileDialogManager = new FileDialogManager();
    }

    public void Dispose() { }

    public override void Draw()
    {
        var items = configuration.ImagePaths;
        
        fileDialogManager.Draw();

        if (ImGui.Button("Add Image"))
        {
            fileDialogManager.OpenFileDialog("Open Image File", ".png, .jpg, .jpeg, .bmp", (success, path) =>
            {
                if (!success) return;
                
                configuration.ImagePaths.Add(path);
                configuration.Save();
            });
        }
        
        if (items.Count == 0) return;
        
        var windowWidth = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
        const int width = 256;
        var spacing = ImGui.GetStyle().ItemSpacing.X;

        var paths = configuration.ImagePaths.ToList();
        for (var i = 0; i < paths.Count; i++)
        {
            var path = paths[i];
            using var id = ImRaii.PushId(i);
            var image = Service.TextureProvider.GetFromFile(path).GetWrapOrDefault();
            if (image is null) continue;

            Vector2 size = new(width, image.Height * width / image.Width);

            if (i > 0 && windowWidth >= ImGui.GetItemRectMax().X + spacing + width)
                ImGui.SameLine();

            ImGui.ImageButton(image.Handle, size);

            using var popup = ImRaii.ContextPopupItem($"image context menu");
            if (popup.Success)
            {
                if (ImGui.MenuItem("Delete"))
                {
                    configuration.ImagePaths.Remove(path);
                    configuration.Save();
                }
            }
        }
    }
}
