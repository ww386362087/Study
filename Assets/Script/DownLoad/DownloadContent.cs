using System;
using System.IO;
using System.Net;
using System.Text;
using NUnit.Framework;
using UnityEngine;
/// <summary>
/// 下载内容 提供最后修改时间 已经下载的大小 等属性
/// </summary>
class DownloadContent
{
    public enum emState
    {
        DownLoading,
        Canceling,
        Completed,
        Failed,
    }
    public const int FILE_LAST_MODIFIED_SIZE = 32;
    public const int BUFFER_SIZE = 1024;
    public const string TEMP_EXTENSION_NAME = ".download";
    /// <summary>
    /// 当前下载状态
    /// </summary>
    public emState State;
    /// <summary>
    /// 文件名
    /// </summary>
    public string FileFullName;
    /// <summary>
    /// 上次已经下载的大小
    /// </summary>
    public long LastTimeCompletedLength;
    /// <summary>
    /// 数据缓存
    /// </summary>
    public byte[] Buffer;
    /// <summary>
    /// 上次最后修改时间 与服务器的文件比较判断是否是断点下载
    /// </summary>
    public DateTime LastModified;

    public FileStream FS;
    public Stream ResponseStream { get; private set; }
    private HttpWebResponse _webResponse;

    public HttpWebResponse WebResponse
    {
        get { return _webResponse; }
        set
        {
            _webResponse = value;
            ResponseStream = _webResponse.GetResponseStream();
        }
    }

    public string TempFullName
    {
        get { return FileFullName + TEMP_EXTENSION_NAME; }
    }

    /// <summary>
    ///  构造函数
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="isnew">是否是新文件(没有下载过)</param>
    /// 
    public DownloadContent(string filename)
    {
        FileFullName = filename;
        State = emState.DownLoading;
        Buffer = new byte[BUFFER_SIZE];
        OpenFile();
    }
    /// <summary>
    /// 获取最后访问时间，上次已经下载的大小
    /// </summary>
    private void OpenFile()
    {
        try
        {
            //首先判断文件夹是否存在  创建文件夹
            string parent = Path.GetDirectoryName(FileFullName);
            if (!Directory.Exists(parent))
            {
                Directory.CreateDirectory(parent);
            }
            if (!File.Exists(TempFullName))
            {
                //以前没有下载过这个文件
                FS = new FileStream(TempFullName,FileMode.Create,FileAccess.ReadWrite);
                LastTimeCompletedLength = 0;
                LastModified = DateTime.MinValue;
            }
            else
            {
                //以前下载过但不知道有没有下载完
                FS = new FileStream(TempFullName,FileMode.OpenOrCreate,FileAccess.ReadWrite);
                LastTimeCompletedLength = FS.Length;
                if (LastTimeCompletedLength > FILE_LAST_MODIFIED_SIZE) //如果长度大于标志位的长度
                {
                    if (ReadDateTime(ref LastModified))
                    {
                        FS.Seek(LastTimeCompletedLength - FILE_LAST_MODIFIED_SIZE, SeekOrigin.Begin);
                        LastTimeCompletedLength = LastTimeCompletedLength - FILE_LAST_MODIFIED_SIZE;
                    }
                }
                else
                {
                    FS.Seek(0,SeekOrigin.Begin);
                    LastModified = DateTime.MinValue;
                    LastTimeCompletedLength = 0;
                }
            }

        }
        catch (Exception e)
        {
            //Console.WriteLine(e);    
            //throw;
            Debug.LogError(e.Message);
        }
        if (FS != null)
        {
            FS.Close();
            FS = null;
        }
    }

    private bool ReadDateTime(ref DateTime time)
    {
        if (FS != null || FS.Length > FILE_LAST_MODIFIED_SIZE)
        {
            FS.Seek(LastTimeCompletedLength - FILE_LAST_MODIFIED_SIZE, SeekOrigin.Begin);
            byte[] bytes  = new byte[FILE_LAST_MODIFIED_SIZE];
            FS.Read(bytes, 0, 32);
            string str = Encoding.UTF8.GetString(bytes);
            long l = long.Parse(str);
            time = new DateTime(l);
            return true;
        }
        return false;
    }

    private void WriteDateTime(DateTime time)
    {
        if (FS != null)
        {
            string str1 = time.Ticks.ToString().PadLeft(FILE_LAST_MODIFIED_SIZE, '0');
            byte[] bytes = Encoding.UTF8.GetBytes(str1);
            FS.Write(bytes,0,bytes.Length);
        }
    }

    private void CloseFile()
    {
        if (FS != null)
        {
            FS.Close();
            FS = null;
        }
        if (File.Exists(TempFullName))
        {
            if (State == emState.Completed)
            {
                if (File.Exists(FileFullName))
                {
                    File.Delete(FileFullName);
                }
                File.Move(TempFullName,FileFullName);
            }
            else
            {
                File.Delete(TempFullName);
            }
        }
    }

    private void CloseFile(DateTime time)
        //if (State == emState.Failed )
    {
        if (State == emState.Failed || State == emState.DownLoading || State == emState.Canceling)
        {
            WriteDateTime(_webResponse.LastModified);
        }
        if (FS != null)
        {
            FS.Close();
            FS = null;
        }
        if (File.Exists(TempFullName))
        {
            if (State == emState.Completed)
            {
                if (File.Exists(FileFullName))
                {
                    File.Delete(FileFullName);
                }
                File.Move(TempFullName,FileFullName);
            }
        }
    }

    public void Close()
    {
        if (_webResponse != null)
        {
            CloseFile(_webResponse.LastModified);
        }
        else
        {
            CloseFile();
        }
        if (ResponseStream != null)
        {
            ResponseStream.Close();
            ResponseStream = null;
        }
        if (_webResponse != null)
        {
            _webResponse.Close();
            _webResponse = null;
        }
    }
    
}
