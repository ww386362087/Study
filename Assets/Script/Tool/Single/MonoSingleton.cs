using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 继承自mono的单例继承泛型接口类
/// </summary>
public class MonoSingleton<T>: MonoBehaviour where T: MonoSingleton<T>
{
	private static T _instance;

	public static T Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = GameObject.FindObjectOfType<T>();
				if (_instance == null)
				{
					_instance = new GameObject("Singletion:::"+typeof(T).ToString()).AddComponent<T>();
					DontDestroyOnLoad(_instance);
				}
			}
			return _instance;
			
		}
	}

	public static T CreateSingleton()
	{
		return Instance;
	}

	public virtual void OnDestroy()
	{
		_instance = null;
	}
}
