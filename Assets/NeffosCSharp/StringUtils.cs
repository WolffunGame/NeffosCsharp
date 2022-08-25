using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace NeffosCSharp
{
    public static class StringUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ToByteArray(this string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        public static string ToUTF8String(this byte[] value)
        {
            return Encoding.UTF8.GetString(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string EscapeMessageField(string message)
        {
            if(string.IsNullOrEmpty(message))
            {
                return string.Empty;
            }

            var escapeRegExg = new Regex(Configuration.messageSeparator);
            return escapeRegExg.Replace(message, Configuration.messageFieldSeparatorReplacement);
        } 
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string UnescapeMessageField(string message)
        {
            if(string.IsNullOrEmpty(message))
            {
                return string.Empty;
            }
            
            var escapeRegExg = new Regex(Configuration.messageFieldSeparatorReplacement);
            return escapeRegExg.Replace(message, Configuration.messageSeparator);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string[] SplitN(string s, string sep, int limit)
        {
            if (string.IsNullOrEmpty(s))
            {
                return Array.Empty<string>();
            }
            if (string.IsNullOrEmpty(sep))
            {
                return new[] { s };
            }
            if (limit == 0)
            {
                return new[] { s };
            }
            
            var parts = s.Split(new[] { sep }, limit, StringSplitOptions.None);
            if (parts.Length == limit)
            {
                return parts;
            }
            var result = new string[limit];
            parts.CopyTo(result, 0);
            return result;
        }


    }
}