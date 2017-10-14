using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;

public static class FileHelper  {
    /// <summary>
    /// 拷贝文件函数
    /// </summary>
    /// <param name="src"></param>
    /// <param name="des"></param>
    /// <param name="isfugai"></param>
    /// <returns></returns>
       public static bool CopyFile(string src,string des,bool isfugai)
       {
           if (!File.Exists(src))
           {
               return false;
           }
           string dirpath = Path.GetDirectoryName(des);
           if (!Directory.Exists(dirpath))
           {
               Directory.CreateDirectory(dirpath);
           }
           File.Copy(src,des,isfugai);
           return true;
       }

    public static bool Isextenname(string filename, string[] exes)
    {
        string ext = Path.GetExtension(filename);
        foreach (string s in exes)
        {
            if (s.Equals(ext))
            {
                return true;
            }
        }
        return false;
    }

    public static bool Isflodername(string filename, string[] flodrs)
    {
        string path = Path.GetFileName(filename);
        foreach (string s in flodrs)
        {
            if (s.Equals(path))
            {
                return true;
            }
        }
        return false;
    }

    public static void WriteFile(string path,string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        int length = bytes.Length;
        string pa = Path.GetDirectoryName(path);
        if (!Directory.Exists(pa))
        {
            Directory.CreateDirectory(pa);
        }
        FileInfo fileInfo = new FileInfo(path);
        using (FileStream fs =fileInfo.Open(FileMode.Append,FileAccess.Write))
        {
            fs.Write(bytes,0,length);
        }
        
    }

    public static int GetAllSize(string filename)
    {
        int sum = 0;
        if (!Directory.Exists(filename))
        {
            return 0;
        }
        DirectoryInfo directoryInfo = new DirectoryInfo(filename);
        FileInfo[] fileInfo = directoryInfo.GetFiles();
        foreach (FileInfo info in fileInfo)
        {
            sum += (int)info.Length / 1024;
        }
        DirectoryInfo[] directoryInfos = directoryInfo.GetDirectories();
        foreach (var dir in directoryInfos)
        {
            sum += GetAllSize(dir.FullName);
        }
        return sum;
    }

    public static long GetFileSize(string filename)
    {
        if (!File.Exists(filename))
        {
            return 0;
        }
        FileInfo fileInfo = new FileInfo(filename);
        return fileInfo.Length;
    }

    static void CreateAssetbundleFile(string path, byte[] bytes, int length)
    {
        using (FileStream fileStream = new FileStream(path,FileMode.Create,FileAccess.Write))
        {
            fileStream.Write(bytes,0,length);
        }
    }

    static IEnumerator ReadAssetBundleLocal(string path,string name,Action<UnityEngine.Object> callback)
    {
        path = "file:///" + path + "/" + name;
        using (WWW ww = new WWW(path))
        {
            yield return ww;
            if (ww.error != null)
            {
                Debug.Log(ww.error);
            }
            else
            {
                if (callback != null)
                {
                    callback(ww.assetBundle.mainAsset);
                }
            }
        }
    }

    static void DeleteFile(string path,string filename)
    {
        File.Delete(path+"/"+filename);
    }

    static void DeleteFileandDir(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }
        else
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            FileInfo[] fileInfos = directoryInfo.GetFiles("*");
            DirectoryInfo[] directoryInfos = directoryInfo.GetDirectories("*");
            foreach (FileInfo fileInfo in fileInfos)
            {
                File.Delete(fileInfo.FullName);
            }
            foreach (DirectoryInfo info in directoryInfos)
            {
                Directory.Delete(info.FullName,true);
            }
        }
    }

    static string Getquchukuozhna(string name)
    {
        int index = name.LastIndexOfAny(".".ToCharArray());
        return name.Substring(0, index);
    }

    static string Pathzhuanhuan(string abpath)
    {
        return "";
    }

    public static bool CopyDirectoryAllChildren(string scr_folder, string dest_folder, string[] ignore_extensions = null, string[] ignore_folders = null, bool is_cover = false, System.Action<string> notify_callback = null)
    {
        string[] files = Directory.GetFiles(scr_folder);
        foreach (string file in files)
        {
            string desfloder = dest_folder + Path.GetFileName(file);
            File.Copy(file,desfloder,true);
            string[] dirc = Directory.GetDirectories(scr_folder);
            foreach (string s in dirc)
            {
                CopyDirectoryAllChildren(s, desfloder + Path.GetDirectoryName(s));
            }
        }
        return false;
    }

}
