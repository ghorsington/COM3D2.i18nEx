using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace COM3D2.i18nEx.Core.Util
{
    internal class KeyCommand : IDisposable
    {
        public static readonly Func<KeyCommand, string> KeyCommandToString =
            kc => string.Join("+", kc.KeyCodes.Select(k => k.ToString()).ToArray());

        public static readonly Func<string, KeyCommand> KeyCommandFromString = s =>
            new KeyCommand(s.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(k => (KeyCode)Enum.Parse(typeof(KeyCode), k, true)).ToArray());

        public KeyCommand(params KeyCode[] keyCodes)
        {
            KeyCodes = keyCodes;
            KeyStates = new bool[KeyCodes.Length];

            KeyCommandHandler.Register(this);
        }

        private KeyCode[] KeyCodes { get; }
        private bool[] KeyStates { get; }

        public bool IsPressed => KeyStates.All(k => k);

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        public void UpdateState()
        {
            for (var i = 0; i < KeyCodes.Length; i++)
            {
                var key = KeyCodes[i];
                KeyStates[i] = Input.GetKey(key);
            }
        }

        private void ReleaseUnmanagedResources()
        {
            KeyCommandHandler.Unregister(this);
        }

        ~KeyCommand()
        {
            ReleaseUnmanagedResources();
        }
    }

    internal static class KeyCommandHandler
    {
        public static List<KeyCommand> KeyCommands = new();

        public static void Register(KeyCommand command)
        {
            KeyCommands.Add(command);
        }

        public static void Unregister(KeyCommand command)
        {
            KeyCommands.Remove(command);
        }

        public static void UpdateState()
        {
            foreach (var keyCommand in KeyCommands)
                keyCommand.UpdateState();
        }
    }
}
