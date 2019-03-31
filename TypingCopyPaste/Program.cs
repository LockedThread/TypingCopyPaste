using System;
using System.Diagnostics;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace TypingCopyPaste
{
    internal class Program
    {
        [STAThread]
        public static void Main()
        {
            var kh = new KeyboardHook(true);
            kh.KeyUp += Kh_KeyDown;
            Application.Run();
        }

        private static void Kh_KeyDown(Keys key, bool shift, bool ctrl, bool alt)
        {
            if (key != Keys.F8) return;
            StartWritingClipBoard();
        }

        private static void StartWritingClipBoard()
        {
            var clipboardWriter = new ClipboardWriter(Clipboard.GetText(), 0) {Interval = 10, AutoReset = true};
            clipboardWriter.Elapsed += clipboardWriter.Tick;
            clipboardWriter.Start();
        }
    }

    internal class ClipboardWriter : Timer
    {
        private readonly string _fullString;
        private int _currentIndex;

        public ClipboardWriter(string fullString, int currentIndex)
        {
            _fullString = fullString;
            _currentIndex = currentIndex;
        }

        public void Tick(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            Debug.WriteLine("Index = "+_currentIndex);
            if (_currentIndex>=_fullString.Length)
            {
                Stop();
                Debug.WriteLine("Unable to write index of " + _currentIndex + " due to being out of bounds");
                return;
            }
            var c = _fullString[_currentIndex];
            Debug.WriteLine(c.ToString());
            SendKeys.SendWait(c.ToString());
            _currentIndex++;
        }
    }
}
