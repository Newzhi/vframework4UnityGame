using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 基于 CDN 清单 hash 比对与 HTTP 下载的远程 Bundle 提供者（C-2 基础实现）。
/// </summary>
public sealed class HttpRemoteBundleProvider : IRemoteBundleProvider
{
    readonly string cdnBaseUrl;
    readonly string localBundleRoot;
    readonly CatalogueReader catalogueReader;

    AssetCatalog remoteCatalog;
    string remoteCatalogueHash;

    /// <param name="cdnBaseUrl">CDN 根 URL，末尾无斜杠。</param>
    /// <param name="localBundleRoot">本地 bundle 缓存目录（与 IBundlePathResolver 一致）。</param>
    /// <param name="catalogueReader">已加载本地清单的 Reader，用于比对 catalogueHash。</param>
    public HttpRemoteBundleProvider(string cdnBaseUrl, string localBundleRoot, CatalogueReader catalogueReader)
    {
        this.cdnBaseUrl = (cdnBaseUrl ?? string.Empty).TrimEnd('/');
        this.localBundleRoot = localBundleRoot;
        this.catalogueReader = catalogueReader;
    }

    public bool EnsureBundle(string bundleName)
    {
        bundleName = BundlePlatformPaths.NormalizeBundleName(bundleName);
        if (string.IsNullOrEmpty(bundleName))
            return false;

        if (!EnsureRemoteCatalogSynced())
            return false;

        string localPath = ResolveLocalBundlePath(bundleName);
        if (IsLocalBundleValid(bundleName, localPath))
            return true;

        return DownloadBundle(bundleName, localPath);
    }

    bool EnsureRemoteCatalogSynced()
    {
        if (string.IsNullOrEmpty(cdnBaseUrl))
        {
            Debug.LogError("[HttpRemoteBundleProvider] cdnBaseUrl 为空");
            return false;
        }

        string remoteCatalogUrl = cdnBaseUrl + "/Catalogue/" + CatalogueReader.RuntimeCatalogueFileName;
        if (!TryHttpGetText(remoteCatalogUrl, out string remoteJson))
            return false;

        AssetCatalog parsed = JsonUtility.FromJson<AssetCatalog>(remoteJson);
        if (parsed == null)
        {
            Debug.LogError("[HttpRemoteBundleProvider] 远程清单解析失败");
            return false;
        }

        remoteCatalog = parsed;
        remoteCatalogueHash = parsed.catalogueHash;

        string localHash = catalogueReader?.Catalog?.catalogueHash;
        if (!string.IsNullOrEmpty(localHash)
            && string.Equals(localHash, remoteCatalogueHash, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        Debug.Log("[HttpRemoteBundleProvider] 清单已更新，remote buildId=" + parsed.buildId);
        return true;
    }

    bool IsLocalBundleValid(string bundleName, string localPath)
    {
        if (!File.Exists(localPath))
            return false;

        if (remoteCatalog?.bundles == null)
            return true;

        foreach (BundleCatalogInfo info in remoteCatalog.bundles)
        {
            if (info == null || !string.Equals(info.bundleName, bundleName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (info.sizeBytes > 0 && new FileInfo(localPath).Length != info.sizeBytes)
                return false;

            if (!string.IsNullOrEmpty(info.fileHash))
            {
                string hash = BundleIntegrityUtil.ComputeFileSha256(localPath);
                if (!string.Equals(hash, info.fileHash, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (info.crc32 != 0)
            {
                uint crc = BundleIntegrityUtil.ComputeFileCrc32(localPath);
                if (crc != info.crc32)
                    return false;
            }

            return true;
        }

        return true;
    }

    bool DownloadBundle(string bundleName, string localPath)
    {
        string url = cdnBaseUrl + "/" + bundleName;
        if (!TryHttpGetBytes(url, out byte[] data))
            return false;

        string dir = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllBytes(localPath, data);

        if (!IsLocalBundleValid(bundleName, localPath))
        {
            Debug.LogError("[HttpRemoteBundleProvider] 下载后校验失败: " + bundleName);
            if (File.Exists(localPath))
                File.Delete(localPath);
            return false;
        }

        Debug.Log("[HttpRemoteBundleProvider] 已下载: " + bundleName);
        return true;
    }

    string ResolveLocalBundlePath(string bundleName)
    {
        if (!string.IsNullOrEmpty(localBundleRoot))
            return Path.Combine(localBundleRoot, bundleName);

        return Path.Combine(Application.persistentDataPath, "vFramework", "Bundles", bundleName);
    }

    static bool TryHttpGetText(string url, out string text)
    {
        text = null;
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 30;
            var operation = request.SendWebRequest();
            while (!operation.isDone) { }

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                Debug.LogError("[HttpRemoteBundleProvider] GET 失败: " + url + " | " + request.error);
                return false;
            }

            text = request.downloadHandler.text;
            return !string.IsNullOrEmpty(text);
        }
    }

    static bool TryHttpGetBytes(string url, out byte[] bytes)
    {
        bytes = null;
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 60;
            var operation = request.SendWebRequest();
            while (!operation.isDone) { }

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                Debug.LogError("[HttpRemoteBundleProvider] 下载失败: " + url + " | " + request.error);
                return false;
            }

            bytes = request.downloadHandler.data;
            return bytes != null && bytes.Length > 0;
        }
    }
}
