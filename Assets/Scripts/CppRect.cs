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

    public override int GetHashCode()
    {
        return System.HashCode.Combine(x, y, w, h);
    }

    public override bool Equals(object obj)
    {
        if (obj is CppRect other)
        {
            return x.Equals(other.x) && y.Equals(other.y) &&
                w.Equals(other.w) && h.Equals(other.h);
        }

        return false;
    }
}