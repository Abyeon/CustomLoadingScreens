using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Timelines;

namespace CustomLoadingScreens;

// Basically a re-implementation of Kami's version of FancyLoadingScreens
// https://github.com/MidoriKami/VanillaPlus/blob/master/VanillaPlus/Features/FancyLoadingScreens/FancyLoadingScreens.cs
public class LoadingScreenManager : IAsyncDisposable
{
    private Hook<Telepo.Delegates.Teleport>? teleportHook;

    private AddonController? locationTitleController;
    private TimelineNode<ImGuiImageNode>? artworkImageNode;
    private bool isTeleporting;

    private Configuration configuration = null!;

    public async Task LoadAsync(Configuration config)
    {
        configuration = config;
        
        unsafe
        {
            teleportHook = Service.GameInteropProvider.HookFromAddress<Telepo.Delegates.Teleport>(Telepo.MemberFunctionPointers.Teleport, OnTeleport);
        }
        
        await Service.Framework.Run(teleportHook.Enable);

        unsafe
        {
            locationTitleController = new AddonController
            {
                AddonName = "_LocationTitle",
                OnSetup = OnLocationTitleSetup,
                OnDraw = OnLocationTitleDraw,
                OnFinalize = OnLocationTitleFinalize
            };
        }
        
        await locationTitleController.EnableAsync();
        
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostHide, "_LocationTitle", OnLoadingScreenHide);
        
        Service.Log.Verbose($"Loaded LoadingScreenManager");

        Service.ClientState.TerritoryChanged += OnTerritoryChanged;
    }

    private unsafe void OnLocationTitleSetup(AtkUnitBase* addon)
    {
        artworkImageNode = new TimelineNode<ImGuiImageNode>
        {
            ContentNode =
            {
                FitTexture = true
            },

            LabelsetTimeline = new TimelineBuilder()
                               .BeginFrameSet(1, 480)
                               .AddLabel(1, 1, AtkTimelineJumpBehavior.Start, 0)
                               .AddLabel(480, 0, AtkTimelineJumpBehavior.PlayOnce, 0)
                               .EndFrameSet()
                               .Build(),

            ContentTimeline = new TimelineBuilder()
                              .BeginFrameSet(1, 480)
                              .AddFrame(1, scale: new Vector2(1, 1), alpha: 0)
                              .AddFrame(60, scale: new Vector2(1.1f, 1.1f), alpha: 255)
                              .AddFrame(480, scale: new Vector2(1.4f, 1.4f), alpha: 255)
                              .EndFrameSet()
                              .Build()
        };
        
        artworkImageNode.AttachNode(addon, NodePosition.AsFirstChild);
        
        GetNextImage();
    }
    
    private unsafe void OnLocationTitleDraw(AtkUnitBase* addon)
    {
        var screenSize = (Vector2)AtkStage.Instance()->ScreenSize;
        var rootScale = new Vector2(addon->RootNode->GetScaleX(), addon->RootNode->GetScaleY());
        var position = new Vector2(addon->RootNode->X, addon->RootNode->Y);
        
        artworkImageNode?.Position = -position / rootScale;
        artworkImageNode?.Size = screenSize / rootScale;
        artworkImageNode?.ContentNode.Origin = screenSize / rootScale / 2.0f;
    }
    
    private unsafe void OnLocationTitleFinalize(AtkUnitBase* addon)
    {
        artworkImageNode?.Dispose();
        artworkImageNode = null;
    }

    private void SetLoadingScreenImage()
    {
        Service.Log.Verbose($"Trying to set loading screen image");
        
        if (configuration.ImagePaths.Count == 0) return;
        if (artworkImageNode is null) return;
        
        artworkImageNode?.Timeline?.PlayAnimation(1, true);
        artworkImageNode?.IsVisible = true;
    }
    
    private void OnLoadingScreenHide(AddonEvent type, AddonArgs args)
    {
        artworkImageNode?.IsVisible = false;
        isTeleporting = false;
        
        GetNextImage();
    }

    private void GetNextImage()
    {
        var index = Random.Shared.Next(0, configuration.ImagePaths.Count);
        var path = configuration.ImagePaths[index];
        
        Service.Log.Verbose($"Loading image {path}");
        artworkImageNode?.ContentNode.TexturePath = path;
    }

    private void OnTerritoryChanged(uint obj)
    {
        if (!isTeleporting) SetLoadingScreenImage();
    }
    
    private unsafe bool OnTeleport(Telepo* thisPtr, uint aetheryteId, byte subIndex)
    {
        var accepted = teleportHook!.Original(thisPtr, aetheryteId, subIndex);

        try {
            if (accepted)
            {
                isTeleporting = true;
                SetLoadingScreenImage();
            }
        }
        catch (Exception exception) {
            Service.Log.Error("Error while detouring OnTeleport", exception);
        }

        return accepted;
    }

    public async ValueTask DisposeAsync()
    {
        Service.AddonLifecycle.UnregisterListener(OnLoadingScreenHide);

        if (teleportHook != null) await Service.Framework.Run(teleportHook.Dispose);
        teleportHook = null;

        if (locationTitleController != null) await locationTitleController.DisposeAsync();
        locationTitleController = null;
    }
}
