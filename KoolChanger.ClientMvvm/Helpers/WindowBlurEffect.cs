#region

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Effects;

#endregion

namespace KoolChanger.Helpers;

//ty f1den for this $$SUPERIOR$$ blur effect
internal class WindowBlurEffect
{
    private readonly uint _blurBackgroundColor = 0x990000;

    private uint _blurOpacity;
    private KernelType _kernelType;

    //to call blur in our desired window
    internal WindowBlurEffect(Window window, AccentState accentState, KernelType kernelType = KernelType.Box)
    {
        _window = window;
        _accentState = accentState;
        _kernelType = kernelType;
        EnableBlur();
    }

    public double BlurOpacity
    {
        get => _blurOpacity;
        set
        {
            _blurOpacity = (uint)value;
            EnableBlur();
        }
    }

    private Window _window { get; }
    private AccentState _accentState { get; }

    [DllImport("user32.dll")]
    internal static extern int SetWindowCompositionAttribute(nint hwnd, ref WindowCompositionAttributeData data);

    internal void EnableBlur()
    {
        var windowHelper = new WindowInteropHelper(_window);
        var accent = new AccentPolicy();


        accent.AccentState = _accentState;
        accent.GradientColor = (_blurOpacity << 24) | (_blurBackgroundColor & 0xFFFFFF);


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
}