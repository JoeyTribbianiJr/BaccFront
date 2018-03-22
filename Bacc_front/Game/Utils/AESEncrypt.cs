using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WsUtils
{
    public class AESEncrypt
    {

        public static string FormatKey(string key)
        {
            if (key.Length < 17)
            {
                key = key.PadLeft(16, '0');
            }
            return key.Substring(0, 16);
        }
        /// <summary>

        /// AES加密

        /// </summary>

        /// <param name="encryptStr">明文</param>

        /// <param name="key">密钥</param>

        /// <returns></returns>

        public static string Encrypt(string encryptStr, string key)
        {
            key = FormatKey(key);

            byte[] keyArray = Encoding.UTF8.GetBytes(key);

            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(encryptStr);


            RijndaelManaged rDel = new RijndaelManaged
            {
                Key = keyArray,

                Mode = CipherMode.ECB,

                Padding = PaddingMode.PKCS7
            };

            ICryptoTransform cTransform = rDel.CreateEncryptor();

            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);


            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        /// <summary>

        /// AES解密

        /// </summary>

        /// <param name="decryptStr">密文</param>

        /// <param name="key">密钥</param>

        /// <returns></returns>

        public static string Decrypt(string decryptStr, string key)
        {
            key = FormatKey(key);

            byte[] keyArray = Encoding.UTF8.GetBytes(key);

            byte[] toEncryptArray = Convert.FromBase64String(decryptStr);

            RijndaelManaged rDel = new RijndaelManaged
            {
                Key = keyArray,

                Mode = CipherMode.ECB,

                Padding = PaddingMode.PKCS7
            };


            ICryptoTransform cTransform = rDel.CreateDecryptor();

            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return Encoding.UTF8.GetString(resultArray);
        }

    }
}
