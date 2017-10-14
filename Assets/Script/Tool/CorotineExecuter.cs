using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CorotineExecuter : MonoBehaviour
{

	public Action DoneCallBalc;

	void Start()
	{
		DontDestroyOnLoad(this.gameObject);
	}

	void Do(AsyncOperation asyncOperation, Action callback)
	{
		DoneCallBalc = callback;
		StartCoroutine(_waitForDone(asyncOperation));
	}

	IEnumerator _waitForDone(AsyncOperation asyncOperation)
	{
		if (asyncOperation == null)
		{
			yield return null;
		}
		while (!asyncOperation.isDone)
		{
			yield return 1;
		}
		if (DoneCallBalc != null)
		{
			DoneCallBalc();
		}
		Destroy(this.gameObject);
		yield return 0;
	}

	void Do(IEnumerator iEnumerator, Action callback)
	{
		DoneCallBalc = callback;
		StartCoroutine(_waitForDone(iEnumerator));
	}

	IEnumerator _waitForDone(IEnumerator iEnumerable)
	{
		if (iEnumerable != null)
		{
			yield return iEnumerable;
		}
		if (DoneCallBalc != null)
		{
			DoneCallBalc();
		}
		Destroy(this.gameObject);
		yield return 0;
	}

	public static void Create(AsyncOperation asyncOperation, Action action)
	{
		GameObject gameObject = new GameObject("CrortineGameobject");
		CorotineExecuter corotineExecuter = gameObject.AddComponent<CorotineExecuter>();
		if (corotineExecuter != null)
		{
			corotineExecuter.Do(asyncOperation,action);
		}
	}

	public static void Create(IEnumerator iEnumerator,Action action)
	{
		GameObject gameObject = new GameObject();
		CorotineExecuter corotineExecuter = gameObject.AddComponent<CorotineExecuter>();
		if (corotineExecuter != null)
		{
			corotineExecuter.Do(iEnumerator,action);
		}
	}
}
