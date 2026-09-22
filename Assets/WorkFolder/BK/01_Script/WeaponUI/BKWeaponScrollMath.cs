using System;

namespace BK.WeaponUI
{
    // No Unity dependency: the circular layout can be checked without running a scene.
    internal static class BKWeaponScrollMath
    {
        public static double Wrap(double value, double length)
        {
            return value - Math.Floor(value / length) * length;
        }

        public static double CycleLength(int count, double visibleRows, double largestRowHeight,
            double largestScale, double padding)
        {
            // Both ends of the cycle must be outside the image, even for the largest row.
            return Math.Max(count / visibleRows, 1 + largestRowHeight * largestScale + 2 * padding);
        }

        public static double RowY(int index, int count, double offset, double cycleLength)
        {
            double spacing = cycleLength / count;
            double bottom = (1 - cycleLength) * 0.5;
            return bottom + Wrap(1 - (index + 0.5) * spacing + offset * spacing - bottom, cycleLength);
        }

        public static float ScaleAt(double y, float top, float bottom)
        {
            double t = Math.Max(0, Math.Min(1, 1 - y));
            t = t * t * (3 - 2 * t);
            return (float)(top + (bottom - top) * t);
        }

        public static void Advance(ref double offset, ref double pending, int count,
            double response, double speedLimit, double seconds, bool dragging)
        {
            if (dragging)
            {
                pending = 0;
                return;
            }

            if (seconds <= 0 || Math.Abs(pending) < 0.000001)
                return;

            double amount = pending * (1 - Math.Exp(-response * seconds));
            if (Math.Abs(pending) < 0.001)
                amount = pending;
            double limit = speedLimit * seconds;
            amount = Math.Max(-limit, Math.Min(limit, amount));
            pending -= amount;
            offset = Wrap(offset + amount, count);
        }
    }
}
