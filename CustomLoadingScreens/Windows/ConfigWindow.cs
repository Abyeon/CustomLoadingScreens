using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;

namespace CustomLoadingScreens.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    
    private FileDialogManager fileDialogManager;
    
    public ConfigWindow(Plugin plugin) : base("Custom Loading Screens###CustomLoadingScreensConfig")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(200, 600)
        };

        configuration = plugin.Configuration;
        
        fileDialogManager = new FileDialogManager();
    }

    public void Dispose() { }

    private string selectedImagePath = string.Empty;

    private const int ThumbnailWidth = 256;
    private const int LargeImageWidth = 1024;
    
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
        
        ImGui.Spacing();
        
        if (items.Count == 0) return;
        
        var windowWidth = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
        var spacing = ImGui.GetStyle().ItemSpacing.X;

        bool openLargeImagePopup = false;

        var paths = configuration.ImagePaths.ToList();
        for (var i = 0; i < paths.Count; i++)
        {
            var path = paths[i];
            using var id = ImRaii.PushId(i);
            var image = Service.TextureProvider.GetFromFile(path).GetWrapOrDefault();
            if (image is null) continue;

            Vector2 size = new(ThumbnailWidth, image.Height * ThumbnailWidth / image.Width);

            if (i > 0 && windowWidth >= ImGui.GetItemRectMax().X + spacing + ThumbnailWidth)
                ImGui.SameLine();

            if (ImGui.ImageButton(image.Handle, size))
            {
                selectedImagePath = path;
                openLargeImagePopup = true;
            }

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

        if (openLargeImagePopup)
            ImGui.OpenPopup("LargeImagePopup");
        
        DrawLargeImagePopup();
    }

    private void DrawLargeImagePopup()
    {
        if (selectedImagePath.IsNullOrEmpty()) return;
        
        var selectedImage = Service.TextureProvider.GetFromFile(selectedImagePath).GetWrapOrDefault();
        if (selectedImage is null) return;
        
        var center = ImGui.GetMainViewport().GetCenter();
        Vector2 selectedSize = new(LargeImageWidth, selectedImage.Height * LargeImageWidth / selectedImage.Width);
        
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        using var largePopup = ImRaii.Popup("LargeImagePopup", ImGuiWindowFlags.AlwaysAutoResize);
        if (!largePopup.Success) return;

        ImGui.Image(selectedImage.Handle, selectedSize);
    }
}
