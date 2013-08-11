/*****************
 
    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License version 2 as published by
    the Free Software Foundation

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA

*******************/


using System;
using System.Collections.Generic;
using System.Text;
using com.amazon.s3;
using System.Security.Cryptography;
using System.IO;
using System.Net;


namespace jdcmd
{
	class JungleDiskCmd
	{
		static string accessKey;
		static string secretKey;
		static string command;
		static string bucket;
		static string path;
		static string localfile;
		static AWSAuthConnection conn;

		static void PrintUsage()
		{
			Console.WriteLine("Usage: jdcmd.exe <AccessKeyID> <SecretKeyID> <Command>");
			Console.WriteLine("Available Commands:");
			Console.WriteLine(" listbuckets - Displays a list of all available buckets");
			Console.WriteLine(" dir <bucket> <path> - Displays a list of files in the specified bucket and path");
			Console.WriteLine(" getfile <bucket> <path> <localfile.ext> - Retrieves the file at the specified bucket and path");
		}

		static void Main(string[] args)
		{
			if (args.Length < 3)
			{
				PrintUsage();
				return;
			}
			accessKey = args[0];
			secretKey = args[1];
			command = args[2];
			if (command != "listbuckets")
			{
				if (args.Length < 5)
				{
					PrintUsage();
					return;
				}
				bucket = Md5Hash(accessKey) + "-" + args[3];
				path = args[4];
			}
			if (command == "getfile" || command == "putfile")
			{
				if (args.Length < 6)
				{
					PrintUsage();
					return;
				}
				localfile = args[5];
			}
			try
			{

				conn = new AWSAuthConnection(accessKey, secretKey);
				switch (command)
				{
					case "listbuckets":
						ListBuckets();
						break;
					case "dir":
						GetDirectoryListing();
						break;
					case "getfile":
						GetFile();
						break;
					case "listkeys":
						GetKeyListing();
						break;
					default:
						PrintUsage();
						break;
				}

			} catch (Exception ex)
			{
				Console.WriteLine("Error executing command: {0}\n{1}", ex.Message, ex.ToString());
			}
		}

		static void ListBuckets()
		{
			ListAllMyBucketsResponse resp = conn.listAllMyBuckets(null);
			string bucketPrefix = Md5Hash(accessKey) + "-";
			foreach (Bucket bucket in resp.Buckets)
			{
				//see if it matches our pattern
				if (bucket.Name.StartsWith(bucketPrefix, StringComparison.InvariantCulture))
					Console.WriteLine("Found bucket: {0}", bucket.Name.Remove(0,bucketPrefix.Length));
			}

		}

		static void GetKeyListing()
		{
			string marker = null;
			ListBucketResponse resp;
			do
			{
				resp = conn.listBucket(bucket, "", marker, 0, null);
				marker = null;
				foreach (ListEntry entry in resp.Entries)
				{
					Console.WriteLine("{0}", entry.Key);
					marker = entry.Key;
				}
			} while (marker != null);
		}


		static void GetDirectoryListing()
		{
			string marker = null;
			ListBucketResponse resp;
			string prefix;
			//add the trailing slash if needed
			if (!path.EndsWith("/"))
				path += "/";
			prefix = GetFilename(path, null);
			do
			{
				resp = conn.listBucket(bucket, prefix, marker, 0, null);
				marker = null;
				foreach (ListEntry entry in resp.Entries)
				{
					bool isDirectory;
					string filePath = GetPathFromName(entry.Key, out isDirectory);
					Console.WriteLine("{0} ({1})", filePath, isDirectory ? "dir" : "file");
					marker = entry.Key;
				}
				
			} while (marker != null);
		}

		enum EncryptionType { etNone, etRC4, etAES };

		static byte[] AESCTREncrypt(ICryptoTransform aes, byte[] buffer, int bufferLen, byte[] ivec, byte[] evec, ref int blockOffset, ref int blockIndex)
		{			
			byte[] ret = new byte[bufferLen];
			for (int i = 0 ; i < bufferLen ; i++)
			{
				if (blockOffset == 0) //calculate the encrypted block
				{
					//increment the ivec as if it were a 128-bit big endian number
					byte[] newIvec = (byte[])ivec.Clone();
					long val = BitConverter.ToInt64(ivec, 8);
					val = IPAddress.HostToNetworkOrder(IPAddress.NetworkToHostOrder(val) + blockIndex);
					byte[] valarray = BitConverter.GetBytes(val);
					valarray.CopyTo(newIvec, 8);
					aes.TransformBlock(newIvec, 0, newIvec.Length, evec, 0);
					blockIndex++;
				}
				ret[i] = (byte)(buffer[i] ^ evec[blockOffset]);
				blockOffset = (blockOffset + 1) % aes.OutputBlockSize;
			}
			return ret;
		}

		
		static void GetFile()
		{
			EncryptionType et;
			GetResponse resp = conn.get(bucket, GetFilename(path, ".file"), null);
			string encryptionMethod = resp.Connection.Headers["x-amz-meta-crypt"];
			string encryptionSalt = resp.Connection.Headers["x-amz-meta-crypt-salt"];
			if (encryptionSalt == null)
				encryptionSalt = path.ToLower();
			else
				encryptionSalt = Uri.UnescapeDataString(encryptionSalt);
			if (encryptionMethod.StartsWith("rc4"))
				et = EncryptionType.etRC4;
			else if (encryptionMethod.StartsWith("aes"))
				et = EncryptionType.etAES;
			else et = EncryptionType.etNone;

			Stream s = resp.GetResponseStream();
			RC4 rc4 = new RC4();
			string keyString = secretKey + encryptionSalt;
			Rijndael aes = RijndaelManaged.Create();
			int blockIndex = 0;
			int blockOffset = 0;
			byte[] ivec = null;
			byte[] evec = null;
			byte[] key;
			if (et != EncryptionType.etNone)
			{
				int keylen = (et == EncryptionType.etAES) ? 32 : 16;
				key = new byte[keylen];
				rc4.EVP_BytesToKey(new byte[0], Encoding.Default.GetBytes(keyString), 1, key, new byte[0]);
				if (et == EncryptionType.etAES)
				{
					aes.Mode = CipherMode.ECB;
					aes.Key = key;
					blockIndex = 0;
					blockOffset = 0;
					ivec = (new MD5CryptoServiceProvider()).ComputeHash(Encoding.Default.GetBytes(encryptionSalt));
					evec = new byte[ivec.Length];

				} else
				{
					rc4.Init(key);
				}
			}

			using (FileStream fs = File.OpenWrite(localfile))
			{
				byte[] buffer = new byte[1024];
				while (true)
				{
					int nread = s.Read(buffer, 0, buffer.Length);
					if (nread == 0)
						return;
					byte[] decrypt = (et == EncryptionType.etAES) ? AESCTREncrypt(aes.CreateEncryptor(), buffer, nread, ivec, evec, ref blockIndex, ref blockOffset) :
						(et == EncryptionType.etRC4) ? rc4.Encrypt(buffer, nread) : buffer;
					fs.Write(decrypt, 0, nread);
				}
			}			
		}
		

		//convert a filename to a path name
		static string GetPathFromName(string fileName, out bool isDirectory)
		{
			isDirectory = false;
			int lastPeriod = fileName.LastIndexOf('.');
			if (lastPeriod == -1)
				return null;
			string suffix = fileName.Substring(lastPeriod);
			if (suffix == ".file")
				isDirectory = false;
			else if (suffix == ".dir")
				isDirectory = true;
			else
				return null;
			StringBuilder path = new StringBuilder();
			int firstPeriod = fileName.IndexOf('.');
			for (int i = firstPeriod ; i < lastPeriod ; i++)
			{
				if (fileName[i] == '_')
				{
					if (fileName[i + 1] == '_')
					{
						path.Append('_');
						i++;
					} else if (fileName[i + 1] == '-')
					{
						path.Append('.');
						i++;
					} else
					{
						path.Append((char)(Convert.ToByte(fileName.Substring(i + 1, 2), 16)));
						i += 2;
					}

				} else if (fileName[i] == '.')
					path.Append('/');
				else
					path.Append(fileName[i]);
			}
			return path.ToString();

		}

		//convert a path name to a file name with the provided suffix
		static string GetFilename(string path, string suffix)
		{
			//calculate the depth
			int depth = 0;
			foreach (char c in path)
				if (c == '/')
					depth++;
			StringBuilder val = new StringBuilder();
			val.Append(depth);
			foreach (byte b in Encoding.Default.GetBytes(path))
			{
				if (b == (byte)'.')
					val.Append("_-");
				else if (b == (byte)'/')
					val.Append('.');
				else if (b == (byte)'_')
					val.Append("__");
				else if (b < 32 || (b >= 58 && b <= 64) || b > 126  || b == (byte)'*' || b == (byte)'"')
				{
					val.AppendFormat("_{0:x2}", b);
				} else
					val.Append((char)b);
			}
			if (suffix != null)
				val.Append(suffix);
			return val.ToString();

		}

		public static string Md5Hash(string str)
		{
			MD5 md5 = new MD5CryptoServiceProvider();

			byte[] result = md5.ComputeHash(Encoding.Default.GetBytes(str));
			StringBuilder sb = new StringBuilder();
			for (int i = 0 ; i < result.Length ; i++)
				sb.Append(result[i].ToString("x"));
			return sb.ToString();
		}
		
	}
}
