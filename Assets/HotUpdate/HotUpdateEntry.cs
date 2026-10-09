using UnityEngine;

public class HotUpdateEntry
{
    /// <summary>
    /// 这是热更程序集的入口方法，主工程加载完 DLL 后会反射调用此方法
    /// </summary>
    public static void Main()
    {
        Debug.Log("<color=green>[HotUpdate] === 热更新 C# 代码启动成功！===</color>");

        // 测试调用 Unity API 创建一个场景对象
        GameObject testGo = new GameObject("HotUpdate_TestObject");
        Debug.Log($"[HotUpdate] 成功创建场景 GameObject: {testGo.name}");
    }
}