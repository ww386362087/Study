using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework.Constraints;
using SevenZip.Buffer;
using SevenZip.Compression.LZMA;
using UnityEngine;

public class YaSuoJieYaSuo {

	static bool LAMPyasuo(string filein,string fileout,string houzhui = ".7z")
	{
		try
		{
			if (!File.Exists(filein))
			{
				return false;
			}
			//读入文件流 输出文件流
			FileStream fileinStream = new FileStream(filein,FileMode.Open,FileAccess.Read);
			FileStream fileoutStream = new FileStream(fileout,FileMode.OpenOrCreate,FileAccess.Write);
			Encoder encoder = new Encoder();
			encoder.WriteCoderProperties(fileoutStream);//写压缩信息
			byte[] bytes = BitConverter.GetBytes(fileinStream.Length);//获取传入数据文件的字节长度数组 
			fileoutStream.Write(bytes,0,bytes.Length);
			encoder.Code(fileinStream,fileoutStream,fileinStream.Length,-1,null);
			fileoutStream.Flush();
			fileoutStream.Close();
			fileinStream.Close();
			return true;
		}
		catch (Exception e)
		{
			//Console.WriteLine(e);
			throw;
		}
		return false;
	}

	static bool LAMPjieyasuo(string filein, string outfile)
	{
		try
		{
			if (!File.Exists(filein))
			{
				return false;
				
			}
			FileStream insStream = new FileStream(filein,FileMode.Open);
			FileStream outStream = new FileStream(outfile,FileMode.OpenOrCreate);
			//duqu properties
			byte[] properies = new byte[5];
			insStream.Read(properies, 0, 5);
			
			byte[] filelength = new byte[8];
			insStream.Read(filelength, 0, 8);
			long length = BitConverter.ToInt64(filelength, 0);
			Decoder decoder = new Decoder();
			decoder.SetDecoderProperties(properies);
			
			decoder.Code(insStream,outStream,insStream.Length,length,null);
			outStream.Flush();
			outStream.Close();
			insStream.Close();
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
		return false;
	}
}
