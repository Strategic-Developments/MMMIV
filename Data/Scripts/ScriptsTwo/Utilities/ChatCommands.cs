using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRage;
using VRage.Game.ModAPI.Ingame.Utilities;
using Sandbox.Definitions;

namespace Meridian.Utilities
{

    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class ChatCommands : MySessionComponentBase
    {
        public static string ModName = "Meridian";
        private static Dictionary<string, MyTuple<Action<ulong, string>, string, MyPromoteLevel>> _chatCommands = new Dictionary<string, MyTuple<Action<ulong, string>, string, MyPromoteLevel>>();

        public static void OnChatMessageRecieved(ulong sender, string messageText, ref bool sendToOthers)
        {
            string[] messageTextSplit = messageText.Split(' ');

            MyTuple<Action<ulong, string>, string, MyPromoteLevel> Command;

            if (_chatCommands.TryGetValue(messageTextSplit[0].ToLowerInvariant(), out Command))
            {
                sendToOthers = false;

                if (MyAPIGateway.Session.GetUserPromoteLevel(sender) >= Command.Item3)
                {
                    Command.Item1.Invoke(sender, messageText);
                }
                else
                {
                    ShowMessage($"Error: No permission to run command '{messageTextSplit[0]}'");
                }
            }
        }



        public static void AddChatCommand(string CommandText, Action<ulong, string> Command, string description = null, MyPromoteLevel minUserRequirement = MyPromoteLevel.None)
        {
            if (!_chatCommands.ContainsKey(CommandText.ToLowerInvariant()))
                _chatCommands.Add(CommandText.ToLowerInvariant(), new MyTuple<Action<ulong, string>, string, MyPromoteLevel>(Command, description, minUserRequirement));
            else
            {
                MyLog.Default.Warning("Chat command already exists.");
            }
        }
        public static void ChatCommand_GetAllCommands(ulong SenderId, string message)
        {
            foreach (KeyValuePair<string, MyTuple<Action<ulong, string>, string, MyPromoteLevel>> Command in _chatCommands)
            {
                if (!(Command.Key == "/showallcommands") && MyAPIGateway.Session.GetUserPromoteLevel(SenderId) >= Command.Value.Item3)
                {
                    if (Command.Value.Item2 != null)
                        ShowMessage(Command.Value.Item2);
                    else
                        ShowMessage(Command.Key.ToString());
                }
            }

            return;
        }
        public override void BeforeStart()
        {
            MyAPIUtilities.Static.MessageEnteredSender += OnChatMessageRecieved;
            AddChatCommand("/ShowAllCommands", ChatCommand_GetAllCommands);
        }

        protected override void UnloadData()
        {
            MyAPIUtilities.Static.MessageEnteredSender -= OnChatMessageRecieved;
            _chatCommands.Clear();
        }

        public static void ShowMessage(string message)
        {
            MyAPIGateway.Utilities.ShowMessage(ModName, message);
        }


        public static bool IsOwner(ulong PlayerId)
        {
            return MyAPIGateway.Session.GetUserPromoteLevel(PlayerId) >= MyPromoteLevel.Owner;
        }
        public static bool IsAdmin(ulong PlayerId)
        {
            return MyAPIGateway.Session.GetUserPromoteLevel(PlayerId) >= MyPromoteLevel.Admin;
        }

        public static bool IsSpaceMaster(ulong PlayerId)
        {
            return MyAPIGateway.Session.GetUserPromoteLevel(PlayerId) >= MyPromoteLevel.SpaceMaster;
        }

        public static bool IsModerator(ulong PlayerId)
        {
            return MyAPIGateway.Session.GetUserPromoteLevel(PlayerId) >= MyPromoteLevel.Moderator;
        }

        public static bool IsOnlyPlayer(ulong PlayerId)
        {
            return MyAPIGateway.Session.GetUserPromoteLevel(PlayerId) == MyPromoteLevel.None;
        }
    }
}
