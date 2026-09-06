using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace RPGOverlay;

public class HudRegionNotification : HudElement
{
    private GuiElementHoverText textElem;
    private readonly Vec4f fadeCol = new(1f, 1f, 1f, 1f);
    private long textActiveMs;
    private const int DurationVisibleMs = 5000;
    private readonly Queue<string> messageQueue = new();

    public override string ToggleKeyCombinationCode => null;
    public override bool Focusable => false;

    public HudRegionNotification(ICoreClientAPI capi) : base(capi)
    {
        capi.Event.RegisterGameTickListener(OnTick, 20);
        // PlayerEntitySpawn fires when the local player is in-world and ready
        capi.Event.PlayerEntitySpawn += _ => ComposeGui();
        RPGOverlayModSystem.Logger.Log("[RegionHUD] HudRegionNotification created, waiting for PlayerEntitySpawn");
    }

    private void ComposeGui()
    {
        RPGOverlayModSystem.Logger.Log("[RegionHUD] ComposeGui called");

        ElementBounds containerBounds = new ElementBounds
        {
            Alignment = EnumDialogArea.CenterTop,
            BothSizing = ElementSizing.Fixed,
            fixedWidth = 700.0,
            fixedHeight = 5.0,
            fixedY = 80.0
        };
        ElementBounds textBounds = ElementBounds.Fixed(0.0, 0.0, 700.0, 40.0);

        CairoFont font = CairoFont.WhiteMediumText()
            .WithFont(GuiStyle.DecorativeFontName)
            .WithColor(GuiStyle.DiscoveryTextColor)
            .WithStroke(GuiStyle.DialogBorderColor, 2.0)
            .WithOrientation(EnumTextOrientation.Center);

        ClearComposers();
        Composers["regionnotif"] = capi.Gui.CreateCompo("regionnotif", containerBounds.FlatCopy())
            .PremultipliedAlpha(enable: false)
            .BeginChildElements(containerBounds)
            .AddTranspHoverText("", font, 700, textBounds, "regiontext")
            .EndChildElements()
            .Compose();

        textElem = Composers["regionnotif"].GetHoverText("regiontext");
        textElem.SetFollowMouse(on: false);
        textElem.SetAutoWidth(on: false);
        textElem.SetAutoDisplay(on: false);
        textElem.fillBounds = true;
        textElem.RenderColor = fadeCol;
        textElem.ZPosition = 60f;
        textElem.RenderAsPremultipliedAlpha = false;

        TryOpen();
        RPGOverlayModSystem.Logger.Log("[RegionHUD] ComposeGui done, HUD is open");
    }

    public void Show(string text)
    {
        RPGOverlayModSystem.Logger.Log($"[RegionHUD] Show called: '{text}', textElem null={textElem == null}");
        messageQueue.Enqueue(text);
    }

    private void OnTick(float _)
    {
        if (textElem == null) return;
        if (textActiveMs == 0L && messageQueue.Count == 0) return;

        if (textActiveMs == 0L)
        {
            string msg = messageQueue.Dequeue();
            RPGOverlayModSystem.Logger.Log($"[RegionHUD] Displaying: '{msg}'");
            textActiveMs = capi.InWorldEllapsedMilliseconds;
            fadeCol.A = 0f;
            textElem.SetNewText(msg);
            textElem.SetVisible(on: true);
            return;
        }

        long elapsed = capi.InWorldEllapsedMilliseconds - textActiveMs;
        long remaining = DurationVisibleMs - elapsed;

        if (remaining <= 0)
        {
            textActiveMs = 0L;
            textElem.SetVisible(on: false);
            return;
        }

        fadeCol.A = elapsed < 250 ? (float)elapsed / 240f : 1f;
        if (remaining < 1000) fadeCol.A = (float)remaining / 990f;
    }

    public override bool TryClose() => false;
    public override bool ShouldReceiveKeyboardEvents() => false;
    public override bool ShouldReceiveMouseEvents() => false;
}
