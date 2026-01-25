using UnityEngine;

namespace Match3.Unity.Services
{
    /// <summary>
    /// Utility for parsing hex color strings.
    /// </summary>
    public static class ConfigColorUtility
    {
        /// <summary>
        /// Parse hex color string to Unity Color.
        /// Supports #RGB, #RGBA, #RRGGBB, #RRGGBBAA formats.
        /// </summary>
        public static Color ParseHexColor(string hex, Color defaultColor = default)
        {
            if (string.IsNullOrEmpty(hex))
                return defaultColor;

            // Remove # prefix if present
            if (hex.StartsWith("#"))
                hex = hex.Substring(1);

            try
            {
                return hex.Length switch
                {
                    // #RGB
                    3 => new Color(
                        ParseHexDigit(hex[0]) / 15f,
                        ParseHexDigit(hex[1]) / 15f,
                        ParseHexDigit(hex[2]) / 15f),

                    // #RGBA
                    4 => new Color(
                        ParseHexDigit(hex[0]) / 15f,
                        ParseHexDigit(hex[1]) / 15f,
                        ParseHexDigit(hex[2]) / 15f,
                        ParseHexDigit(hex[3]) / 15f),

                    // #RRGGBB
                    6 => new Color(
                        ParseHexByte(hex, 0) / 255f,
                        ParseHexByte(hex, 2) / 255f,
                        ParseHexByte(hex, 4) / 255f),

                    // #RRGGBBAA
                    8 => new Color(
                        ParseHexByte(hex, 0) / 255f,
                        ParseHexByte(hex, 2) / 255f,
                        ParseHexByte(hex, 4) / 255f,
                        ParseHexByte(hex, 6) / 255f),

                    _ => defaultColor
                };
            }
            catch
            {
                return defaultColor;
            }
        }

        private static int ParseHexDigit(char c)
        {
            return c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'A' and <= 'F' => c - 'A' + 10,
                >= 'a' and <= 'f' => c - 'a' + 10,
                _ => 0
            };
        }

        private static int ParseHexByte(string hex, int offset)
        {
            return ParseHexDigit(hex[offset]) * 16 + ParseHexDigit(hex[offset + 1]);
        }
    }
}
