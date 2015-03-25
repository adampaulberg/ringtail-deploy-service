using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCamel.Helpers
{
    // Code taken from Ringtail.Legal.Common.Security
    public class SecurityHelper
    {
        private const string RingtailPrivateKey = "ps2XSoN2Y";

        public static string Decrypt(string encryptedString)
        {
            return Decrypt(encryptedString, RingtailPrivateKey);
        }

        /// <summary>
        /// Decodes a given string using a "Rollover" algorithm and using the given key
        /// </summary>
        /// <param name="encryptedString">String encrypted using the private key</param>
        /// <param name="privateKey">private key used for Ringtal encoding</param>
        /// <returns>Decoded string</returns>
        public static string Decrypt(string encryptedString, string privateKey)
        {
            char[] unscrambledbuffer = new char[0];
            if (!string.IsNullOrEmpty(encryptedString))
            {
                if (string.IsNullOrEmpty(privateKey))
                {
                    privateKey = RingtailPrivateKey;
                }
                unscrambledbuffer = new char[encryptedString.Length];
                for (int i = 0; i < unscrambledbuffer.Length; i++)
                {
                    int keyCode = Strings.Asc(encryptedString[i]) - (Strings.Asc(privateKey[(i + 1) % privateKey.Length]) - 0x40);
                    if (keyCode < 0x20)
                    {
                        keyCode += 0x5b;
                    }
                    unscrambledbuffer[i] = (char)keyCode;
                }
            }
            return new string(unscrambledbuffer);
        }




        public static string Decode(string encryptedString)
        {
            return Decode(encryptedString, RingtailPrivateKey);
        }


        public static string Decode(string encryptedString, string privateKey)
        {
            char[] unscrambledbuffer = new char[0];
            if (!string.IsNullOrEmpty(encryptedString))
            {
                if (string.IsNullOrEmpty(privateKey))
                {
                    privateKey = RingtailPrivateKey;
                }
                unscrambledbuffer = new char[encryptedString.Length];
                for (int i = 0; i < unscrambledbuffer.Length; i++)
                {
                    int keyCode = Strings.Asc(encryptedString[i]) - (Strings.Asc(privateKey[(i + 1) % privateKey.Length]) - 0x40);
                    if (keyCode < 0x20)
                    {
                        keyCode += 0x5b;
                    }
                    unscrambledbuffer[i] = (char)keyCode;
                }
            }
            return new string(unscrambledbuffer);
        }
    }
}
