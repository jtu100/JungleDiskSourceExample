/* vim: set expandtab shiftwidth=4 softtabstop=4 tabstop=4: */

/**
 * RC4.NET 1.0
 *
 * RC4.NET is a petite library that allows you to use RC4
 * encryption easily in the .NET Platform. It's OO and can
 * produce outputs in binary and hex.
 *
 * (C) Copyright 2006 Mukul Sabharwal [http://mjsabby.com]
 *	   All Rights Reserved
 *
 * @link http://rc4dotnet.devhome.org
 * @author Mukul Sabharwal <mjsabby@gmail.com>
 * @version $Id: RC4.cs,v 1.0 2006/03/19 15:35:24 mukul Exp $
 * @copyright Copyright &copy; 2006 Mukul Sabharwal
 * @license http://www.gnu.org/copyleft/lesser.html
 * @package RC4.NET
 */

using System;
using System.Text;
using System.Security.Cryptography;


/**
 * RC4 Class
 * @package RC4.NET
 */
namespace jdcmd
{
	public class RC4
	{
		public void EVP_BytesToKey(byte[] salt, byte[] data, int count, byte[] key, byte[] iv)
		{
			int addmd = 0;
			byte[] inBuffer = new byte[data.Length + 16 + salt.Length];
			byte[] md_buf = null;
			int i;
			int keypos = 0;
			int ivpos = 0;

			int nkey = key.Length;
			int niv = iv.Length;

			while (true)
			{
				MD5 md = MD5CryptoServiceProvider.Create();
				int inPos = 0;
				if (addmd++ > 0)
				{
					md_buf.CopyTo(inBuffer, 0);
					inPos += md_buf.Length;
				}
				data.CopyTo(inBuffer, inPos);
				inPos += data.Length;
				salt.CopyTo(inBuffer, inPos);
				inPos += salt.Length;
				md_buf = md.ComputeHash(inBuffer, 0, inPos);
				i = 0;
				if (nkey > 0)
				{
					while (true)
					{
						if (nkey == 0)
							break;
						if (i == md_buf.Length)
							break;
						key[keypos++] = md_buf[i];
						nkey--;
						i++;
					}
				}
				if (niv > 0 && i != md_buf.Length)
				{
					while (true)
					{
						if (niv == 0)
							break;
						if (i == md_buf.Length)
							break;
						iv[ivpos++] = md_buf[i];
						niv--;
						i++;
					}
				}
				if (nkey == 0 && niv == 0)
					break;
			}
		}


		/**
		 * The symmetric encryption function
		 *
		 * @param string pwd Key to encrypt with (can be binary of hex)
		 * @param string data Content to be encrypted
		 * @param bool ispwdHex Key passed is in hexadecimal or not
		 * @access public
		 * @return string
		 */
		int a, i, j, k, tmp;
		int[] box;
		public void Init(byte[] key)
		{
			box = new int[256];
			for (i = 0 ; i < 256 ; i++)
			{
				box[i] = i;
			}
			for (j = i = 0 ; i < 256 ; i++)
			{
				j = (j + box[i] + key[i % key.Length]) % 256;
				tmp = box[i];
				box[i] = box[j];
				box[j] = tmp;
			}
			a = 0;
			j = 0;
		}

		public byte[] Encrypt(byte[] data, int datalen)
		{

			byte[] cipher;

			cipher = new byte[datalen];


			for (i = 0 ; i < datalen ; i++)
			{
				a = (a + 1) % 256;
				j = (j + box[a]) % 256;
				tmp = box[a];
				box[a] = box[j];
				box[j] = tmp;
				k = box[((box[a] + box[j]) % 256)];
				cipher[i] = (byte)(data[i] ^ k);
			}
			return cipher;
		}

	}
}