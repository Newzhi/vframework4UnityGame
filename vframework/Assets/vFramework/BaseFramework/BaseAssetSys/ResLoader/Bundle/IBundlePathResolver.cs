using System.IO;
using UnityEngine;

public interface IBundlePathResolver
{
    bool TryResolveLocalPath(string bundleName, out string localPath);
    bool IsLocalBundleAvailable(string bundleName);
}

public sealed class DefaultBundlePathResolver : IBundlePathResolver
{
    readonly string primaryRoot;
    readonly string cacheRoot;

    /// <summary>热更缓存根目录（persistentDataPath/ABCache/{平台}）。</summary>
    public string CacheRoot => cacheRoot;

    public DefaultBundlePathResolver(string primaryRoot, string cacheRoot = null)
    {
        this.primaryRoot = primaryRoot;
        this.cacheRoot = cacheRoot;
    }

    public static DefaultBundlePathResolver Create(string primaryRoot)
    {
        return new DefaultBundlePathResolver(primaryRoot, CdnPaths.GetCacheRoot());
    }

    public bool TryResolveLocalPath(string bundleName, out string localPath)
    {
        localPath = null;
        bundleName = BundlePlatformPaths.NormalizeBundleName(bundleName);

        if (!string.IsNullOrEmpty(cacheRoot))
        {
            string cachePath = ResolveBundleFilePath(cacheRoot, bundleName);
            if (FileExistsOrJar(cachePath, cacheRoot))
            {
                localPath = cachePath;
                return true;
            }
        }

        if (!string.IsNullOrEmpty(primaryRoot))
        {
            string primaryPath = ResolveBundleFilePath(primaryRoot, bundleName);
            if (FileExistsOrJar(primaryPath, primaryRoot))
            {
                localPath = primaryPath;
                return true;
            }
        }

        return false;
    }

    public bool IsLocalBundleAvailable(string bundleName)
    {
        return TryResolveLocalPath(bundleName, out _);
    }

    static bool FileExistsOrJar(string path, string root)
    {
        if (StreamingAssetsIO.IsNonFileProtocolPath(root))
            return true;

        return File.Exists(path);
    }

    static string ResolveBundleFilePath(string root, string bundleName)
    {
        string path = StreamingAssetsIO.CombinePath(root, bundleName);
        if (StreamingAssetsIO.IsNonFileProtocolPath(root))
            return path;

        if (File.Exists(path))
            return path;

        if (!Directory.Exists(root))
            return path;

        string fileName = Path.GetFileName(bundleName);
        foreach (string file in Directory.GetFiles(root, "*.bundle"))
        {
            if (string.Equals(Path.GetFileName(file), fileName, System.StringComparison.OrdinalIgnoreCase))
                return file;
        }

        return path;
    }
}

public interface IRemoteBundleProvider
{
    bool EnsureBundle(string bundleName);
}

public sealed class StubRemoteBundleProvider : IRemoteBundleProvider
{
    public bool EnsureBundle(string bundleName)
    {
        Debug.LogWarning("IRemoteBundleProvider not implemented; cannot download bundle: " + bundleName);
        return false;
    }
}
