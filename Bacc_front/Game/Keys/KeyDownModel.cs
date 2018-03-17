using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bacc_front
{
    public class KeyDownModel
    {
        public int keycode { get; set; }
        public float Timer { get; set; }
        public Action Handler { get; set; }
        public bool FirstHandled { get; set; }
        public bool Pressed { get; internal set; }
        public bool IsKey { get; internal set; }

        public KeyDownModel(int key)
        {
            IsKey = false;
            Pressed = false;
            FirstHandled = false;
            keycode = key;
            Timer = 0;
        }
    }
}
