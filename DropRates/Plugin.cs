using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using DropRates.Windows;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina;
using Lumina.Excel.GeneratedSheets;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
namespace DropRates;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService] internal static IPluginLog Logger { get; private set; } = null!;
    [PluginService] internal static IChatGui chat {  get; private set; } = null!;
    [PluginService] internal static IDataManager dataManager { get; private set; } = null!;
    private const string CommandName = "/pmycommand";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("DropRates");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // you might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Adds another button that is doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        chat.CheckMessageHandled += Chat_CheckMessageHandled;
    }

    private void Chat_CheckMessageHandled(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (type != XivChatType.SystemMessage) return;
        if (message != null && message.Payloads != null)
        {
            string textValue = message.TextValue;
            if (textValue.Contains("has been added to the loot list"))//has been added to the loot list
            {
                foreach (Payload payload in message.Payloads)
                {
                    if(payload.Type == PayloadType.Item)
                    {
                        Logger.Debug(Framework.GetServerTime().ToString());
                        ItemPayload item = (ItemPayload)payload;
                        Logger.Debug(item.ItemId.ToString());
                    }
                }
            }
        }

        /*if (message.Payloads.Count > 0 && message.Payloads[0].Type == PayloadType.Item  ) {
            ItemPayload item = (ItemPayload)message.Payloads[0];
        }
        
        throw new System.NotImplementedException();
        */
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        chat.CheckMessageHandled -= Chat_CheckMessageHandled;
        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        ToggleMainUI();
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}
