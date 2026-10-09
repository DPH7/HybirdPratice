using System;
using System.IO;
using System.Reflection;
using UnityEngine;

public class GameLauncher : MonoBehaviour
{
    void Start()
    {
        Debug.Log("[Main] 游戏启动，开始加载热更新 DLL...");

        // 1. 获取 StreamingAssets 下 HotUpdate.dll.bytes 的文件路径
        string dllPath = Path.Combine(Application.streamingAssetsPath, "HotUpdate.dll.bytes");

        if (!File.Exists(dllPath))
        {
            Debug.LogError($"[Main] 未找到热更文件，请检查路径: {dllPath}");
            return;
        }

        // 2. 读取 DLL 的字节流
        byte[] dllBytes = File.ReadAllBytes(dllPath);

        // 3. 动态加载 Assembly 程序集
        Assembly hotUpdateAssembly = Assembly.Load(dllBytes);
        Debug.Log("[Main] Assembly.Load 成功！");

        // 4. 通过反射获取 HotUpdateEntry 类并执行 Main 入口函数
        Type type = hotUpdateAssembly.GetType("HotUpdateEntry");
        MethodInfo mainMethod = type.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);

        if (mainMethod != null)
        {
            mainMethod.Invoke(null, null);
        }
        else
        {
            Debug.LogError("[Main] 未找到 HotUpdateEntry.Main() 入口方法！");
        }
    }
}