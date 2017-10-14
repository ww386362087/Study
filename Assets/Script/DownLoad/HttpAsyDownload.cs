 using System;
 using System.Net;
 using System.Threading;
 using UnityEngine;

/// <summary>
///   Http下载类 
/// </summary>
public class HttpAsyDownload
{
  public enum emErrorCode
  {
      /// <summary>
      /// 未知错误
      /// </summary>
      None,
      /// <summary>
      /// 取消下载
      /// </summary>
      Cancel,
      /// <summary>
      /// 没有响应
      /// </summary>
      NoResponse,
      /// <summary>
      /// 下载出错
      /// </summary>
      DownLoadError,
      /// <summary>
      /// 请求超时
      /// </summary>
      TimeOut,
      /// <summary>
      /// 强制关闭
      /// </summary>
      Abort,
  }

    public const int TIMEOUT_TIME = 20000;
    public string URL { get; private set; }
    public string Root { get; private set; }
    public string LocalName { get; private set; }

    public string FullName
    {
        get
        {
            if (string.IsNullOrEmpty(Root) && string.IsNullOrEmpty(LocalName))
            {
                return Root + "/" + LocalName;
            }
            return null;
        }
    }

    public bool IsDone { get; private set; }
    public emErrorCode ErrorCode;
    /// <summary>
    /// 下载文件的总大小 总长度
    /// </summary>
    public long Length { get; private set; }

    public long CompletedLength { get; private set; }
    /// <summary>
    /// 下载通知回调函数
    /// </summary>
    private Action<HttpAsyDownload, long> notify_callback;
    /// <summary>
    /// 下载出错的回调函数
    /// </summary>
    private Action<HttpAsyDownload> error_callback;
    /// <summary>
    /// 下载内容的实例 用来获得文件的最后修改时间等信息
    /// </summary>
    private DownloadContent Content = null;
    /// <summary>
    /// 保证线程安全的锁对象
    /// </summary>
    private object lock_object = null;
    /// <summary>
    /// wen请求对象实例
    /// </summary>
    private HttpWebRequest _httpWebRequest = null;

    public HttpAsyDownload(string url)
    {
        URL = url;
    }
    /// <summary>
    /// 提供外部调用下载开始接口函数
    /// </summary>
    public void Start(string root,string localname,Action<HttpAsyDownload,long> notify = null,Action<HttpAsyDownload> error = null)
    {
        lock (lock_object)
        {
            //TODO 不知道为什么要先终止下载
            //下载之前先终止
            Root = root;
            LocalName = localname;
            if (notify != null)
            {
                notify_callback = notify;
            }
            if (error != null)
            {
                error_callback = error;
            }
            //设置各种属性的初始值
            IsDone = false;
            ErrorCode = emErrorCode.None;
            Content = new DownloadContent(FullName);
            CompletedLength = 0;
            Length = 0;
            //开始下载
            DownLoad();
        }
    }

    public void Cancel()
    {
        lock (lock_object)
        {
            if (Content != null && Content.State == DownloadContent.emState.DownLoading)
            {
                Content.State = DownloadContent.emState.Canceling;
            }
            else
            {
                IsDone = true;
            }
        }
    }

    private void OnFinish()
    {
        lock (lock_object)
        {
            if (Content != null)
            {
                Content.State = DownloadContent.emState.Completed;
                Content.Close();
                Content = null;
            }
            if (_httpWebRequest != null)
            {
                _httpWebRequest.Abort();
                _httpWebRequest = null;
            }
            IsDone = true;
        }
    }

    private void OnFailed(emErrorCode code)
    {
        lock (lock_object)
        {
            if (Content != null)
            {
                Content.State = DownloadContent.emState.Failed;
                Content.Close();
                Content = null;
            }
            if (_httpWebRequest != null)
            {
                _httpWebRequest.Abort();
                _httpWebRequest = null;
            }
            if (error_callback != null)
            {
                error_callback(this);
            }
        }
    }

    public void Abort()
    {
        if (Content != null && Content.State == DownloadContent.emState.DownLoading)
        {
            //Content.State = DownloadContent.emState.Failed;
            //onfailed 里面更新了下载状态 不用在设置了
            OnFailed(emErrorCode.Abort);
        }
    }

    private void DownLoad()
    {
        //TODO
        try
        {
            _httpWebRequest = HttpWebRequest.Create(URL+LocalName) as HttpWebRequest;
            _httpWebRequest.Timeout = TIMEOUT_TIME;
            _httpWebRequest.KeepAlive = false;
            _httpWebRequest.IfModifiedSince = Content.LastModified;
            IAsyncResult iasAsyncResult = _httpWebRequest.BeginGetResponse(OnresponseCallback, _httpWebRequest);
            RegisterTimeOut(iasAsyncResult.AsyncWaitHandle);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            UnregisterTimeOut();
            OnFailed(emErrorCode.NoResponse);
        }

    }

    private void OnresponseCallback(IAsyncResult ias)
    {
        try
        {
            lock (lock_object)
            {
                HttpWebRequest httpWebRequest = ias.AsyncState as HttpWebRequest;
                HttpWebResponse httpWebResponse = httpWebRequest.EndGetResponse(ias) as HttpWebResponse;
                if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    //以前没有下载过这个文件 初始化数据
                    Length = httpWebResponse.ContentLength;
                    Content.WebResponse = httpWebResponse;
                    // 开始下载
                    BeginRead(OnReadCallbcak);
                }
                else if (httpWebResponse.StatusCode == HttpStatusCode.NotModified)
                {
                    //本地有这个文件 但不知道是不是下载完成的文件
                    //终止此次请求 建立新的请求看是否是完整文件
                    if (_httpWebRequest != null)
                    {
                        _httpWebRequest.Abort();
                        _httpWebRequest = null;
                        // 校验文件的完整性
                        PartialDownLoad();
                        return;
                    }
                }
                else
                {
                    httpWebResponse.Close();
                    OnFailed(emErrorCode.NoResponse);
                    return;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            OnFailed(emErrorCode.DownLoadError);
        }

    }

    private void PartialDownLoad()
    {
        try
        {
            lock (lock_object)
            {
                /*   注意
                 WebRequest.ContentLength属性是Int64型的，但是AddRange方法只接受Int32型的参数，
                 所在我们在分段下载大于2个G的文件时，在大于（Int32.MaxValue）的地方时，我们就无法实现分段下载了，
                 意味着大于2GB之后的文件必须用一个线程一次下完，否则文件大于2GB的部分我们无法下载。
                 */
                _httpWebRequest = HttpWebRequest.Create(URL+LocalName) as HttpWebRequest;
                _httpWebRequest.Timeout = TIMEOUT_TIME;
                _httpWebRequest.KeepAlive = false;
                _httpWebRequest.AddRange((int) Content.LastTimeCompletedLength);
                IAsyncResult iAsyncResult =
                    _httpWebRequest.BeginGetResponse(OnPartialResponseCallback, _httpWebRequest);
                RegisterTimeOut(iAsyncResult.AsyncWaitHandle);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            UnregisterTimeOut();
            OnFailed(emErrorCode.NoResponse);
        }
    }

    private void OnPartialResponseCallback(IAsyncResult iAsyncResult)
    {
        try
        {
            UnregisterTimeOut();
            lock (lock_object)
            {
                HttpWebRequest httpWebRequest = iAsyncResult.AsyncState as HttpWebRequest;
                HttpWebResponse httpWebResponse = httpWebRequest.EndGetResponse(iAsyncResult) as HttpWebResponse;
                if (httpWebResponse.StatusCode == HttpStatusCode.PartialContent)
                {
                    //表示当前是断点下载
                    Length = Content.LastTimeCompletedLength + httpWebResponse.ContentLength;
                    Content.WebResponse = httpWebResponse;
                    // 开始下载文件
                    BeginRead(OnReadCallbcak);
                } else if (httpWebResponse.StatusCode == HttpStatusCode.NotModified)
                {
                    //表示当前是已经完全下载的文件
                    OnFailed(emErrorCode.Abort);
                    return;
                }
                else
                {
                    httpWebRequest.Abort();
                    OnFailed(emErrorCode.NoResponse);
                    return;
                    
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            OnFailed(emErrorCode.DownLoadError);
        }
    }

    private void BeginRead(AsyncCallback callback)
    {
        if (Content == null)
        {
            return;
        }
        if (Content.State == DownloadContent.emState.Canceling)
        {
            OnFailed(emErrorCode.Cancel);
            return;
        }
        Content.ResponseStream.BeginRead(Content.Buffer, 0, DownloadContent.BUFFER_SIZE, OnReadCallbcak,Content);
    }

    private void OnReadCallbcak(IAsyncResult iaAsyncResult)
    {
        try
        {
            lock (lock_object)
            {
                DownloadContent content = iaAsyncResult.AsyncState as DownloadContent;
                if(content.ResponseStream == null)return;
                int read = content.ResponseStream.EndRead(iaAsyncResult);
                if (read > 0)
                {
                    content.FS.Write(content.Buffer, 0, read);
                    content.FS.Flush();
                    CompletedLength += read;
                    if (notify_callback != null)
                    {
                        notify_callback(this, (long) read);
                    }
                }
                else
                {
                    OnFinish();
                    if (notify_callback != null)
                    {
                        notify_callback(this, (long) read);
                    }
                }
                BeginRead(OnReadCallbcak);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            OnFailed(emErrorCode.DownLoadError);
        }
    }
    #region TimeOut

    private RegisteredWaitHandle registeredWaitHandle;
    private WaitHandle waitHandle;

    private void RegisterTimeOut(WaitHandle wait)
    {
        waitHandle = wait;
       registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(wait, Callback, null, TIMEOUT_TIME, true);
    }

    private void UnregisterTimeOut()
    {
        if (registeredWaitHandle != null && waitHandle!=null)
        {
            registeredWaitHandle.Unregister(waitHandle);
        }
    }
    private void Callback(object state, bool timedOut)
    {
        lock (lock_object)
        {
            if (timedOut == true)
            {
                OnFailed(emErrorCode.TimeOut);
            }
            UnregisterTimeOut();
        }   
    }
    #endregion
}