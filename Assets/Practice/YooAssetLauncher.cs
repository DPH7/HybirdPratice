using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using YooAsset;

public class YooAssetLauncher : MonoBehaviour
{
    private bool ifHostTest = false;
    private async void Start()
    {
        // 1. 初始化资源系统
        YooAssets.Initialize();

        // 2. 创建资源包
        ResourcePackage package = YooAssets.CreatePackage("DefaultPackage");
        InitializePackageOperation initializationoperation = null;
        
        if (ifHostTest)
        {
            // 2. 配置本地模拟 CDN 服务的 LoadPath 与 FallbackPath
            string defaultHostServer = "http://127.0.0.1:8080/StandaloneWindows64/DefaultPackage/v1.0";
            string fallbackHostServer = "http://127.0.0.1:8080/StandaloneWindows64/DefaultPackage/v1.0";

            IRemoteService remoteService = new RemoteService(defaultHostServer, fallbackHostServer);
            var createParameters = new HostPlayModeOptions();
            createParameters.BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters();
            createParameters.BuiltinFileSystemParameters.AddParameter(EFileSystemParameter.CopyBuiltinPackageManifest, true);
            createParameters.CacheFileSystemParameters = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(remoteService);
            createParameters.CacheFileSystemParameters.AddParameter(EFileSystemParameter.DownloadMaxConcurrency, 5);
            createParameters.CacheFileSystemParameters.AddParameter(EFileSystemParameter.DownloadMaxRequestPerFrame, 1);
            createParameters.CacheFileSystemParameters.AddParameter(EFileSystemParameter.DownloadWatchdogTimeout, 10);
            
            initializationoperation = package.InitializePackageAsync(createParameters);
            await initializationoperation;
        }
        else
        {
            // 在 YooAsset 中，只要传入默认的编辑器文件系统参数，YooAsset 内部会自动定位模拟路径
            var buildResult = EditorSimulateBuildInvoker.Build("DefaultPackage", (int)EBundleType.VirtualAssetBundle);
            var packageRoot = buildResult.PackageRootDirectory;
            var createParameters = new EditorSimulateModeOptions();
            createParameters.EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
            initializationoperation = package.InitializePackageAsync(createParameters);
            await initializationoperation;
        }
 
        if (initializationoperation.Status != EOperationStatus.Succeeded)
        {
            Debug.LogError($"YooAsset 初始化失败: {initializationoperation.Error}");
        }
        
        var VersionOperation = package.RequestPackageVersionAsync();
        await VersionOperation;
        if (VersionOperation.Status != EOperationStatus.Succeeded)
        {
            Debug.LogWarning(VersionOperation.Error);
            //PatchPackageVersionRequestFailedEvent.SendEventMessage();
        }
        else
        {
            var options = new LoadPackageManifestOptions(VersionOperation.PackageVersion, 60);
            var Manifestoperation = package.LoadPackageManifestAsync(options);
            await Manifestoperation;

            if (Manifestoperation.Status != EOperationStatus.Succeeded)
            {
                Debug.LogWarning(Manifestoperation.Error);
            }
            else
            {
                //加载manifest成功
                var downloadingMaxNum = 10;
                var failedTryAgain = 3;
                var DownloaderOptions = new ResourceDownloaderOptions(downloadingMaxNum, failedTryAgain);
                var downloader = package.CreateResourceDownloader(DownloaderOptions);
                downloader.StartDownload();
                await downloader;

                var loadHandle = package.LoadAssetAsync<TextAsset>(
                    "Assets/HotUpdateDlls/HotUpdate.dll.bytes"
                );
                await loadHandle;
                if (loadHandle.Status == EOperationStatus.Succeeded)
                {
                    TextAsset textAsset = loadHandle.AssetObject as TextAsset;

                    byte[] dllBytes = textAsset.bytes;

                    Debug.Log($"DLL大小: {dllBytes.Length}");
                    
                    Assembly hotUpdateAssembly = Assembly.Load(dllBytes);
                    Type entryType = hotUpdateAssembly.GetType("HotUpdateEntry");
                    if (entryType != null)
                    {
                        MethodInfo entryMethod = entryType.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
                        entryMethod?.Invoke(null, null);
                    }
                }
                else
                {
                    Debug.LogError(loadHandle.Error);
                }
            }
        }
    }
    
    // 远程服务器查询接口实现
    private class RemoteService : IRemoteService
    {
        private readonly string _defaultHostServer;
        private readonly string _fallbackHostServer;

        public RemoteService(string defaultHostServer, string fallbackHostServer)
        {
            _defaultHostServer = defaultHostServer;
            _fallbackHostServer = fallbackHostServer;
        }
        public IReadOnlyList<string> GetRemoteUrls(string fileName)
        {
            List<string> result = new List<string>();
            result.Add($"{_defaultHostServer}/{fileName}");
            result.Add($"{_fallbackHostServer}/{fileName}");
            return result;
        }
    }
}