gcc -shared -fPIC -std=c99 `pkg-config --cflags mono-2` -g NETPlugin.c -o monoscripting.so `pkg-config --libs mono-2`

```CSharp
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

public static class 

public static class World
{
	[MethodImplAttribute(MethodImplOptions.InternalCall)]
	public static extern int GetWidth();
	
	[MethodImplAttribute(MethodImplOptions.InternalCall)]
	public static extern int GetHeight();
	
	[MethodImplAttribute(MethodImplOptions.InternalCall)]
	public static extern int GetLength();
	
	[MethodImplAttribute(MethodImplOptions.InternalCall)]
	public static extern int GetBlock(int x, int y, int z);
}

public static class Plugin
{
	public static void Main()
	{
		Console.WriteLine("AAAA");
		new Thread(PrintIt).Start();
	}
	
	static void PrintIt() {
		Thread.Sleep(10 * 1000);
		Console.WriteLine(World.GetWidth() + " : " + World.GetHeight() + " : " + World.GetLength());
	}
}
```