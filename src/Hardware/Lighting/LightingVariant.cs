using System;

namespace OmenHub.Hardware.Lighting
{
    /// <summary>One experimental way to paint the keyboard / light bar, shown as a numbered button.</summary>
    public sealed class LightingVariant
    {
        public int Number;
        public string Title;
        public Func<string> Execute;
    }
}
