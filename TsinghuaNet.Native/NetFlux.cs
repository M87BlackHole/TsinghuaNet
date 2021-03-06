﻿using System;
using System.Runtime.InteropServices;

namespace TsinghuaNet.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NetFlux
    {
        public IntPtr Username;
        public int UsernameLength;
        public long Flux;
        public long OnlineTime;
        public double Balance;
    }
}
