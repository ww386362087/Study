using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Net;

/// <summary>
///   URL验证器
///   用于验证资源服务器列表中可用的服务器的URL
/// </summary>
public class URLVerifier
{
    public bool IsDone { get; private set; }
    public string URL { get; private set; }
    /// <summary>
    /// 需要验证的URL列表
    /// </summary>
    private List<string> urlList;

    private Thread _thread;

    public URLVerifier(List<string> List)
    {
        urlList = List;
        URL = null;
        IsDone = false;
    }

    /// <summary>
    /// 外界调用验证接口
    /// </summary>
    public void Start()
    {
        if (urlList == null || urlList.Count == 0)
        {
            URL = null;
            IsDone = true;
            return;
        }
        if (_thread == null)
        {
            _thread = new Thread(VerifierList);
            _thread.Start();
        }
    }
    /// <summary>
    /// 终止验证
    /// </summary>
    public void Abort()
    {
        if (_thread != null)
        {
            _thread.Abort();
            _thread = null;
        }
        URL = null;
        IsDone = true;
    }

    private void VerifierList()
    {
        if (urlList == null || urlList.Count == 0)
        {
            return;
        }
        IsDone = false;
        URL = null;
        for (int i = 0; i < urlList.Count; i++)
        {
            if (Verifier(urlList[i]) == true)
            {
                URL = urlList[i];
                break;
            }
        }
        IsDone = true;
    }

    private bool Verifier(string url)
    {
        bool ret = false;
        HttpWebRequest httpWebRequest = null;
        HttpWebResponse httpWebResponse = null;
        try
        {
            httpWebRequest = HttpWebRequest.Create(url) as HttpWebRequest;
            httpWebRequest.KeepAlive = false;
            httpWebRequest.Timeout = 20000;
            httpWebRequest.AllowAutoRedirect = false;
            httpWebRequest.UseDefaultCredentials = true;
            httpWebResponse = httpWebRequest.GetResponse() as HttpWebResponse;
            if (httpWebResponse.StatusCode == HttpStatusCode.OK)
            {
                ret = true;
            }
        }
        catch (Exception e)
        {
            //Console.WriteLine(e);
            //throw;
            Debug.LogError(e.Message);
            ret = false;
        }
        finally
        {
            if (httpWebRequest != null)
            {
                httpWebRequest.Abort();
                httpWebRequest = null;
            }
            if (httpWebResponse != null)
            {
                httpWebResponse.Close();
                httpWebResponse = null;
            }
        }
        return ret;
    }
}
