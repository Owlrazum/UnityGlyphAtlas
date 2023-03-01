using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct CppRect
{
    public ushort x;
    public ushort y;
    public ushort w;
    public ushort h;

    public CppRect(ushort xArg, ushort yArg, ushort wArg, ushort hArg)
    {
        x = xArg; y = yArg; w = wArg; h = hArg;
    }

    public override string ToString()
    {
        return $"{x} {y} {w} {h}";
    }
}