﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace KoolChanger.Helpers
{
    //ty f1den for this $$SUPERIOR$$ blur effect
    class WindowBlurEffect
    {
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(nint hwnd, ref WindowCompositionAttributeData data);
        private uint _blurOpacity;
        public double BlurOpacity
        {
            get { return _blurOpacity; }
            set { _blurOpacity = (uint)value; EnableBlur(); }
        }

        private uint _blurBackgroundColor = 0x990000;

        private Window window { get; set; }
        private AccentState accentState { get; set; }
        internal void EnableBlur()
        {
            var windowHelper = new WindowInteropHelper(window);
            var accent = new AccentPolicy();


            accent.AccentState = accentState;
            accent.GradientColor = _blurOpacity << 24 | _blurBackgroundColor & 0xFFFFFF; 


            var accentStructSize = Marshal.SizeOf(accent);

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }

        //to call blur in our desired window
        internal WindowBlurEffect(Window window, AccentState accentState)
        {
            this.window = window;
            this.accentState = accentState;
            EnableBlur();
        }
    }
}
