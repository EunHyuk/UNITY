// 观察者，注意key为字符串类型

using UnityEngine;
using System.Collections.Generic;
using System.Collections;

// Observer and Subject 
public class Subject 
{
	private Dictionary<string, List<Observer>> observerDic = new Dictionary<string, List<Observer>>();
	
	public void registerObserver(Observer observer, string type)
	{
		List<Observer> obList = null;
		if (!observerDic.ContainsKey(type))
		{
			obList = new List<Observer>();
			observerDic.Add(type, obList);
		}
		if (observerDic.TryGetValue(type, out obList))
		{
			observerDic.TryGetValue(type, out obList);
			obList.Add(observer);
		}
		else
		{
            Debug.LogWarning("Failed to get observer list.");
		}
	}
	
	public void removeObserver(Observer observer, string type)
	{
		List<Observer> obList = null;
		if (observerDic.TryGetValue(type, out obList))
		{
			observer.ReleaseLuaFunc();
			obList.Remove(observer);
		}
		else 
		{
            Debug.LogWarning("Failed to get observer list");
		}
	}
	
	public void notifyObservers(string type, params object[] args)
	{
		List<Observer> obList = null;
		if (observerDic.TryGetValue(type, out obList))
		{
			foreach (Observer observer in obList)
			{
				observer.updateData(args);
			}
		}
		else 
		{
            Debug.LogWarning("Failed to get observer list");
		}
	}
}
