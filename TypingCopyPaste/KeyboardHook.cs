using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TypingCopyPaste
{
    public class KeyboardHook : IDisposable
    {
        public delegate int CallbackDelegate(int code, int w, int l);

        public delegate void LocalKeyEventHandler(Keys key, bool shift, bool ctrl, bool alt);

        public enum HookType
        {
            WH_JOURNALRECORD = 0,
            WH_JOURNALPLAYBACK = 1,
            WH_KEYBOARD = 2,
            WH_GETMESSAGE = 3,
            WH_CALLWNDPROC = 4,
            WH_CBT = 5,
            WH_SYSMSGFILTER = 6,
            WH_MOUSE = 7,
            WH_HARDWARE = 8,
            WH_DEBUG = 9,
            WH_SHELL = 10,
            WH_FOREGROUNDIDLE = 11,
            WH_CALLWNDPROCRET = 12,
            WH_KEYBOARD_LL = 13,
            WH_MOUSE_LL = 14
        }

        public enum KeyEvents
        {
            KeyDown = 0x0100,
            KeyUp = 0x0101,
            SKeyDown = 0x0104,
            SKeyUp = 0x0105
        }

        private readonly bool _global;

        private bool _isFinalized;

        private readonly int _hookId;

        //Start hook
        public KeyboardHook(bool global)
        {
            _global = global;
            CallbackDelegate theHookCb = KeybHookProc;
            if (global)
                _hookId = SetWindowsHookEx(HookType.WH_KEYBOARD_LL, theHookCb,
                    0, //0 for local hook. eller hwnd til user32 for global
                    0); //0 for global hook. eller thread for hooken
            else
                _hookId = SetWindowsHookEx(HookType.WH_KEYBOARD, theHookCb,
                    0, //0 for local hook. or hwnd to user32 for global
                    GetCurrentThreadId()); //0 for global hook. or thread for the hook
        }

        public void Dispose()
        {
            if (_isFinalized) return;
            UnhookWindowsHookEx(_hookId);
            _isFinalized = true;
        }

        public event LocalKeyEventHandler KeyDown;
        public event LocalKeyEventHandler KeyUp;

        [DllImport("user32", CallingConvention = CallingConvention.StdCall)]
        private static extern int SetWindowsHookEx(HookType idHook, CallbackDelegate lpfn, int hInstance, int threadId);

        [DllImport("user32", CallingConvention = CallingConvention.StdCall)]
        private static extern bool UnhookWindowsHookEx(int idHook);

        [DllImport("user32", CallingConvention = CallingConvention.StdCall)]
        private static extern int CallNextHookEx(int idHook, int nCode, int wParam, int lParam);

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int GetCurrentThreadId();

        ~KeyboardHook()
        {
            if (_isFinalized) return;
            UnhookWindowsHookEx(_hookId);
            _isFinalized = true;
        }

        //The listener that will trigger events
        private int KeybHookProc(int Code, int W, int L)
        {
            var LS = new KbdllHookStruct();
            if (Code < 0) return CallNextHookEx(_hookId, Code, W, L);
            try
            {
                if (!_global)
                {
                    if (Code == 3)
                    {

                        var keydownup = L >> 30;
                        switch (keydownup)
                        {
                            case 0:
                                KeyDown?.Invoke((Keys) W, GetShiftPressed(), GetCtrlPressed(), GetAltPressed());
                                break;
                            case -1:
                                KeyUp?.Invoke((Keys) W, GetShiftPressed(), GetCtrlPressed(), GetAltPressed());
                                break;
                        }

                        //System.Diagnostics.Debug.WriteLine("Down: " + (Keys)W);
                    }
                }
                else
                {
                    var kEvent = (KeyEvents) W;

                    var vkCode = Marshal.ReadInt32((IntPtr) L); //Leser vkCode som er de første 32 bits hvor L peker.

                    if (kEvent != KeyEvents.KeyDown &&
                        kEvent != KeyEvents.KeyUp &&
                        kEvent != KeyEvents.SKeyDown &&
                        kEvent != KeyEvents.SKeyUp)
                    {
                    }

                    switch (kEvent)
                    {
                        case KeyEvents.KeyDown:
                        case KeyEvents.SKeyDown:
                            KeyDown?.Invoke((Keys) vkCode, GetShiftPressed(), GetCtrlPressed(), GetAltPressed());
                            break;
                        case KeyEvents.KeyUp:
                        case KeyEvents.SKeyUp:
                            KeyUp?.Invoke((Keys) vkCode, GetShiftPressed(), GetCtrlPressed(), GetAltPressed());
                            break;
                    }
                }
            }
            catch (Exception)
            {
                //Ignore all errors...
            }

            return CallNextHookEx(_hookId, Code, W, L);
        }

        [DllImport("user32.dll")]
        public static extern short GetKeyState(Keys nVirtKey);

        public static bool GetCapslock()
        {
            return Convert.ToBoolean(GetKeyState(Keys.CapsLock)) & true;
        }

        public static bool GetNumlock()
        {
            return Convert.ToBoolean(GetKeyState(Keys.NumLock)) & true;
        }

        public static bool GetScrollLock()
        {
            return Convert.ToBoolean(GetKeyState(Keys.Scroll)) & true;
        }

        public static bool GetShiftPressed()
        {
            int state = GetKeyState(Keys.ShiftKey);
            return state > 1 || state < -1;
        }

        public static bool GetCtrlPressed()
        {
            int state = GetKeyState(Keys.ControlKey);
            return state > 1 || state < -1;
        }

        public static bool GetAltPressed()
        {
            int state = GetKeyState(Keys.Menu);
            return state > 1 || state < -1;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct KbdllHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }
    }
}