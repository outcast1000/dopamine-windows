using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Base;
using Dopamine.Core.Extensions;
using System;
using System.Globalization;
using System.Text;

namespace Dopamine.Core.Utils
{
    public static class FormatUtils
    {
        public static string FormatDuration(long duration)
        {
            var sb = new StringBuilder();

            TimeSpan ts = TimeSpan.FromMilliseconds(duration);

            if (ts.Days > 0)
            {
                return string.Concat(string.Format("{0:n1}", ts.TotalDays), " ", ts.TotalDays < 1.1 ? ResourceUtils.GetString("Language_Day") : ResourceUtils.GetString("Language_Days"));
            }

            if (ts.Hours > 0)
            {
                return string.Concat(string.Format("{0:n1}", ts.TotalHours), " ", ts.TotalHours < 1.1 ? ResourceUtils.GetString("Language_Hour") : ResourceUtils.GetString("Language_Hours"));
            }

            if (ts.Minutes > 0)
            {
                sb.Append(string.Concat(ts.ToString("%m"), " ", ts.Minutes == 1 ? ResourceUtils.GetString("Language_Minute") : ResourceUtils.GetString("Language_Minutes"), " "));
            }

            if (ts.Seconds > 0)
            {
                sb.Append(string.Concat(ts.ToString("%s"), " ", ts.Seconds == 1 ? ResourceUtils.GetString("Language_Second") : ResourceUtils.GetString("Language_Seconds")));
            }

            return sb.ToString();
        }

        public static string FormatTime(TimeSpan ts)
        {
            if (ts.Hours > 0)
            {
                return ts.ToString("hh\\:mm\\:ss");
            }
            else
            {
                return ts.ToString("m\\:ss");
            }
        }

        public static string FormatFileSize(long sizeInBytes, bool showByteSize = true)
        {

            string humanReadableSize = string.Empty;

            if (sizeInBytes >= Constants.GigaByteInBytes)
            {
                humanReadableSize = string.Format("{0:#.#} {1}", (double)sizeInBytes / Constants.GigaByteInBytes, ResourceUtils.GetString("Language_Gigabytes_Short"));
            }
            else if (sizeInBytes >= Constants.MegaByteInBytes)
            {
                humanReadableSize = string.Format("{0:#.#} {1}", (double)sizeInBytes / Constants.MegaByteInBytes, ResourceUtils.GetString("Language_Megabytes_Short"));
            }
            else if (sizeInBytes >= Constants.KiloByteInBytes)
            {
                humanReadableSize = string.Format("{0:#.#} {1}", (double)sizeInBytes / Constants.KiloByteInBytes, ResourceUtils.GetString("Language_Kilobytes_Short"));
            }

            NumberFormatInfo nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            nfi.NumberGroupSeparator = " ";

            if (showByteSize)
            {
                return string.Format("{0} ({1} {2})", humanReadableSize, sizeInBytes.ToString("#,#", nfi), ResourceUtils.GetString("Language_Bytes").ToLower());
            }
            else
            {
                return string.Format("{0}", humanReadableSize);
            }
        }

        public static bool ParseLyricsTime(string input, out TimeSpan result)
        {
            try
            {
                var split = input.Split(':');
                if (split.Length == 0)
                {
                    result = new TimeSpan();
                    return false;
                }
                int minutes = Convert.ToInt32(split[0]);
                string secondsAndMilliseconds = split[1];

                split = secondsAndMilliseconds.Split('.');
                int seconds = Convert.ToInt32(split[0]);
                int milliseconds = split.Length == 1 ? 0 : Convert.ToInt32(split[1]);

                result = TimeSpan.FromMilliseconds(minutes * 60000 + seconds * 1000 + milliseconds);
                return true;
            }
            catch (Exception)
            {
            }

            result = new TimeSpan();
            return false;
        }

        public static string GetSortableString(string originalString, bool removePrefix = false)
        {
            if (string.IsNullOrEmpty(originalString)) return string.Empty;

            string returnString = originalString.ToLower().Trim();

            if (removePrefix)
            {
                try
                {
                    returnString = returnString.TrimStart("the ").Trim();
                }
                catch (Exception)
                {
                    // Swallow
                }
            }

            return returnString;
        }

        public static string TrimValue(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return value.Trim();
            }
            else
            {
                return string.Empty;
            }
        }

        public static string DelimitValue(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return $"{Constants.ColumnValueDelimiter}{value.Trim()}{Constants.ColumnValueDelimiter}";
            }
            else
            {
                return string.Empty;
            }
        }

        public static String GetPrettyDate(long ticks)
        {
            // 1.
            // Get time span elapsed since the date.
            var s = DateTime.Now.Subtract(new DateTime(ticks));

            // 2.
            // Get total number of days elapsed.
            var dayDiff = (int)s.TotalDays;

            // 3.
            // Get total number of seconds elapsed.
            var secDiff = (int)s.TotalSeconds;

            // 4.
            // Don't allow out of range values.
            if (dayDiff < 0)
            {
                return null;
            }

            // 5.
            // Handle same-day times.
            if (dayDiff == 0)
            {
                if (secDiff < 12 * 60 * 60)
                    return new DateTime(ticks).ToString("t");
                /*
                // A.
                // Less than one minute ago.
                if (secDiff < 60)
                {
                    return "just now";
                }               
                // B.
                // Less than 2 minutes ago.
                if (secDiff < 120)
                {
                    return String. "1m ago";
                }
                // C.
                // Less than one hour ago.
                if (secDiff < 3600)
                {
                    return string.Format("{0}m ago",
                        Math.Floor((double)secDiff / 60));
                }
                // D.
                // Less than 2 hours ago.
                if (secDiff < 7200)
                {
                    return "1h ago";
                }
                // E.
                // Less than one day ago.
                if (secDiff < 86400)
                {
                    return string.Format("{0}h ago",
                        Math.Floor((double)secDiff / 3600));
                }
                */
            }
            return new DateTime(ticks).ToString("d");
            /*
            // 6.
            // Handle previous days.
            if (dayDiff == 1)
            {
                return "yesterday";
            }
            if (dayDiff < 7)
            {
                return string.Format("{0}d ago",
                    dayDiff);
            }
            if (dayDiff < 91)
            {
                return string.Format("{0}w ago",
                    Math.Ceiling((double)dayDiff / 7));
            }
            // 7.
            // Handle very old values            
            return new DateTime(ticks).ToShortDateString();
            */
        }
    }
}
