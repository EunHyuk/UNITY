// 观察者，参考Subject.cs

using UnityEngine;
using System.Collections;
using XLua;

[LuaCallCSharp]
public class Observer
{
	private LuaFunction func;

	public Observer(LuaFunction func) {
		this.func = func;
	}

	public void updateData(params object[] args) {
		if (this.func != null) {
			this.func.Call(args);
		}
	}

	public void ReleaseLuaFunc() {
		if (this.func != null) {
			this.func.Dispose();
		}
	}
}
