﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;

namespace QuickDefuse
{
    public class QuickDefuse : BasePlugin
    {
        public override string ModuleName => "QuickDefuse";
        public override string ModuleVersion => "1.0";
        public override string ModuleAuthor => "Sinistral";
        public override string ModuleDescription => "Allowes defuse the bomb by cut the right wire";

        public string PluginPrefix = $"[QuickDefuse]";
        private static Wire _rightWire = Wire.NotDefined;
        private static Wire _triedWire = Wire.NotDefined;
        private static CPlantedC4? plantedBomb;
        private static CCSPlayerController? planterPlayer = null;

        enum Wire
        {
            NotDefined = 0,
            Green = 1,
            Yellow = 2,
            Red = 3,
            Blue = 4,
            Random = 5
        }

        public override void Load(bool hotReload)
        {
            Logger?.LogInformation($"Plugin: {ModuleName} - Version: {ModuleVersion} by {ModuleAuthor}");
            Logger?.LogInformation(ModuleDescription);

            RegisterEventHandler<EventBombBegindefuse>(OnBombBeginDefuse);
            RegisterEventHandler<EventBombAbortdefuse>(OnBombAbortDefuse);

            RegisterEventHandler<EventBombPlanted>(OnBombPlantedCommand);
            RegisterEventHandler<EventBombBeginplant>(OnBombBeginplant);
            RegisterEventHandler<EventBombAbortplant>(OnBombAbortPlant);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            RegisterEventHandler<EventRoundStart>(OnRoundStart);

            AddCommandListener("1", OnKeyPressDefuse);
            AddCommandListener("2", OnKeyPressDefuse);
            AddCommandListener("3", OnKeyPressDefuse);
            AddCommandListener("4", OnKeyPressDefuse);
            AddCommandListener("5", OnKeyPressDefuse);

            AddCommandListener("1", OnKeyPressPlant);
            AddCommandListener("2", OnKeyPressPlant);
            AddCommandListener("3", OnKeyPressPlant);
            AddCommandListener("4", OnKeyPressPlant);
            AddCommandListener("5", OnKeyPressPlant);
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            _rightWire = (Wire)new Random().Next(1, 5);
            plantedBomb = null;
            planterPlayer = null;
            return HookResult.Continue;
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            foreach (var item in MenuManager.GetActiveMenus())
            {
                item.Value.Close();
            }

            return HookResult.Continue;
        }

        public HookResult OnPlayerChat(EventPlayerChat @event, GameEventInfo info)
        {
            var player = Utilities.GetPlayerFromUserid(@event.Userid);

            if (@event?.Text.Trim().ToLower() == "!quickdefuse")
            {
                player?.PrintToChat("For quick defuse, you need to bind a key, for example: if you want to cut wire 1 with the K key, type this in the console: bind k \"1\"");
                return HookResult.Handled;
            }

            return HookResult.Continue;
        }

        private void ShowSelectionMenu(CCSPlayerController player, bool isPlant)
        {
            int menuTimeoutSec = 10;
            var menu = new CenterHtmlMenu($"Choose a wire ({menuTimeoutSec}s)", this);

            menu.AddMenuOption(Wire.Green.ToString(), isPlant ? GreenPlantAction : GreenDefuseAction);
            menu.AddMenuOption(Wire.Yellow.ToString(), isPlant ? YellowPlantAction : YellowDefuseAction);
            menu.AddMenuOption(Wire.Red.ToString(), isPlant ? RedPlantAction : RedDefuseAction);
            menu.AddMenuOption(Wire.Blue.ToString(), isPlant ? BluePlantAction : BlueDefuseAction);
            menu.AddMenuOption(Wire.Random.ToString(), isPlant ? RandomPlantAction : RandomDefuseAction);
            MenuManager.OpenCenterHtmlMenu(this, player, menu);

            Task.Run(() =>
            {
                for (int i = menuTimeoutSec; i > 0; --i)
                {
                    menu.Title = $"Choose a wire ({i}s)";
                    Task.Delay(1000).Wait();
                }
                MenuManager.CloseActiveMenu(player);
                if (isPlant) 
                {
                    player.PrintToChat($"You chose {_rightWire}!");
                }

            });
        }

        #region Plant

        private HookResult OnBombBeginplant(EventBombBeginplant @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid)
                return HookResult.Continue;

            planterPlayer = player;

            ShowSelectionMenu(player, true);

            return HookResult.Continue;
        }

        private HookResult OnKeyPressPlant(CCSPlayerController? player, CommandInfo command)
        {
            if (player is not null && planterPlayer is not null && player == planterPlayer)
            {
                int selectedOption = command.GetCommandString switch
                {
                    "1" => 1,
                    "2" => 2,
                    "3" => 3,
                    "4" => 4,
                    "5" => 5,
                    _ => 5
                };

                switch (selectedOption)
                {
                    case 1: GreenPlantAction(player, null); break;
                    case 2: YellowPlantAction(player, null); break;
                    case 3: RedPlantAction(player, null); break;
                    case 4: BluePlantAction(player, null); break;
                    case 5: RandomPlantAction(player, null); break;
                }
            }
            return HookResult.Continue;
        }

        private HookResult OnBombPlantedCommand(EventBombPlanted @event, GameEventInfo info)
        {
            Server.PrintToChatAll($"The bomb can be defused by cutting the right wire (1-4).");
            return HookResult.Continue;
        }

        private static void GreenPlantAction(CCSPlayerController player, ChatMenuOption option)
        {
            _rightWire = Wire.Green;
            MenuManager.CloseActiveMenu(player);
            player.PrintToChat($"You chose {_rightWire}!");
        }

        private static void YellowPlantAction(CCSPlayerController player, ChatMenuOption option)
        {
            _rightWire = Wire.Yellow;
            MenuManager.CloseActiveMenu(player);
            player.PrintToChat($"You chose {_rightWire}!");
        }

        private static void RedPlantAction(CCSPlayerController player, ChatMenuOption option)
        {
            _rightWire = Wire.Red;
            MenuManager.CloseActiveMenu(player);
            player.PrintToChat($"You chose {_rightWire} !");
        }

        private static void BluePlantAction(CCSPlayerController player, ChatMenuOption option)
        {
            _rightWire = Wire.Blue;
            MenuManager.CloseActiveMenu(player);
            player.PrintToChat($"You chose {_rightWire} !");
        }

        private static void RandomPlantAction(CCSPlayerController player, ChatMenuOption option)
        {
            Random rand = new();
            _rightWire = (Wire)rand.Next(1, 5);
            MenuManager.CloseActiveMenu(player);
            player.PrintToChat($"You chose {_rightWire}!");
        }

        private HookResult OnBombAbortPlant(EventBombAbortplant @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid)
                return HookResult.Continue;

            planterPlayer = null;
            _rightWire = Wire.NotDefined;
            MenuManager.GetActiveMenu(player)?.Close();

            return HookResult.Continue;
        }

        #endregion

        #region Defuse

        private HookResult OnBombAbortDefuse(EventBombAbortdefuse @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid)
                return HookResult.Continue;

            _triedWire = Wire.NotDefined;
            MenuManager.GetActiveMenu(player)?.Close();

            return HookResult.Continue;
        }

        private HookResult OnBombBeginDefuse(EventBombBegindefuse @event, GameEventInfo info)
        {
            if (@event.Userid == null || !@event.Userid.IsValid)
            {
                return HookResult.Continue;
            }

            var player = @event.Userid;

            plantedBomb = FindPlantedBomb();
            if (plantedBomb is null) 
            {
                return HookResult.Continue;
            }

            ShowSelectionMenu(player, false);

            return HookResult.Continue;
        }

        private HookResult OnKeyPressDefuse(CCSPlayerController? player, CommandInfo command)
        {
            if (player is not null && plantedBomb is not null && plantedBomb.BeingDefused && plantedBomb.BombDefuser == player.Pawn)
            {
                int selectedOption = command.GetCommandString switch
                {
                    "1" => 1,
                    "2" => 2,
                    "3" => 3,
                    "4" => 4,
                    "5" => 5,
                    _ => 5
                };

                switch (selectedOption)
                {
                    case 1: CutBombWire(Wire.Green); break;
                    case 2: CutBombWire(Wire.Yellow); break;
                    case 3: CutBombWire(Wire.Red); break;
                    case 4: CutBombWire(Wire.Blue); break;
                    case 5:
                        {
                            Random rand = new();
                            CutBombWire((Wire)rand.Next(1, 5));
                            break;
                        }
                }
            }

            return HookResult.Continue;
        }

        private static void GreenDefuseAction(CCSPlayerController player, ChatMenuOption option)
        {
            CutBombWire(Wire.Green);
        }

        private static void YellowDefuseAction(CCSPlayerController player, ChatMenuOption option)
        {
            CutBombWire(Wire.Yellow);
        }

        private static void RedDefuseAction(CCSPlayerController player, ChatMenuOption option)
        {
            CutBombWire(Wire.Red);
        }

        private static void BlueDefuseAction(CCSPlayerController player, ChatMenuOption option)
        {
            CutBombWire(Wire.Blue);
        }

        private static void RandomDefuseAction(CCSPlayerController player, ChatMenuOption option)
        {
            Random rand = new();
            CutBombWire((Wire)rand.Next(1, 5));
        }

        private static void CutBombWire(Wire triedWire)
        {
            if (plantedBomb is null)
            {
                return;
            }

            _triedWire = triedWire;
            if (_rightWire == _triedWire)
            {
                Server.NextFrame(() =>
                {
                    Server.PrintToChatAll($"Bomb has been defused by cutting the right {_rightWire} wire!");
                    plantedBomb.DefuseCountDown = 0;
                    plantedBomb.BombDefused = true;
                });
            }
            else
            {
                Server.PrintToChatAll($"Tried wire was {_triedWire}, but the right wire was {_rightWire}");
                plantedBomb.CannotBeDefused = true;
                plantedBomb.C4Blow = 1;
            }
        }

        private CPlantedC4? FindPlantedBomb()
        {
            var plantedBombList = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4");

            if (!plantedBombList.Any())
            {
                Logger?.LogWarning("No planted bomb entities have been found!");
                return null;
            }

            return plantedBombList.FirstOrDefault();
        }

        #endregion




    }
}