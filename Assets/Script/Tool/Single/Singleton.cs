using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton <T> where T:class 
{
	private static T _instance;
	static Singleton()
	{
		return;
	}
	public static T Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Activator.CreateInstance<T>();
			}
			return _instance;
		}
	}

	public static void Destory()
	{
		_instance = null;
	}

}
