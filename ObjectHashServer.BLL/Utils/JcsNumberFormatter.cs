using System;
using System.Globalization;
using ObjectHashServer.BLL.Exceptions;

namespace ObjectHashServer.BLL.Utils
{
    /// <summary>
    /// Implements RFC 8785 (JCS) Section 3.2.2 Number Canonicalization (ECMAScript 2015 ES6 ToString(Number) rules).
    /// </summary>
    public static class JcsNumberFormatter
    {
        public static string FormatNumber(double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d))
            {
                throw new BadRequestException($"The number value '{d}' (NaN or Infinity) is not supported in JSON.");
            }

            // ECMAScript spec: -0 is represented as "0"
            if (d == 0.0)
            {
                return "0";
            }

            // If integer value within long range, format as integer without decimal point
            if (d % 1 == 0 && d >= long.MinValue && d <= long.MaxValue)
            {
                long longVal = (long)d;
                return longVal.ToString(CultureInfo.InvariantCulture);
            }

            // Convert to string using invariant culture with shortest IEEE 754 representation
            string str = d.ToString(CultureInfo.InvariantCulture);

            // Replace uppercase 'E' with lowercase 'e' for exponential notation
            if (str.Contains('E'))
            {
                str = str.Replace('E', 'e');
            }

            // Ensure exponent sign '+' or '-' is present and leading zeros in exponent are removed
            int eIdx = str.IndexOf('e');
            if (eIdx != -1)
            {
                if (str[eIdx + 1] != '-' && str[eIdx + 1] != '+')
                {
                    str = str.Insert(eIdx + 1, "+");
                }

                char sign = str[eIdx + 1];
                string expDigits = str.Substring(eIdx + 2).TrimStart('0');
                if (string.IsNullOrEmpty(expDigits))
                {
                    expDigits = "0";
                }

                str = str.Substring(0, eIdx + 1) + sign + expDigits;
            }

            return str;
        }
    }
}
