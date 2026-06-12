using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class CdnBundleAssetProvider : IAssetProvider
{
    readonly IBundlePathResolver pathResolver;
    readonly IRemoteBundleProvider remoteProvider;

    public CdnBundleAssetProvider(IBundlePathResolver pathResolver, IRemoteBundleProvider remoteProvider)
    {
        this.pathResolver = pathResolver;
        this.remoteProvider = remoteProvider;
    }

    public AssetSource Source => AssetSource.NETCDN;

    public Object Load(ref AssetLoadContext ctx)
    {
        string bundleName = BundlePlatformPaths.NormalizeBundleName(ctx.bundleName);

        if (pathResolver != null && !pathResolver.IsLocalBundleAvailable(bundleName))
        {
            if (remoteProvider == null || !remoteProvider.EnsureBundle(bundleName))
            {
                Debug.LogError("NETCDN load failed, bundle not available locally or remotely: " + bundleName);
                return null;
            }
        }

        if (ctx.acquiredBundleNames == null)
            ctx.acquiredBundleNames = new List<string>();

        ctx.acquiredBundleNames.Clear();
        AssetBundle bundle = BundleManager.AcquireBundleWithDependencies(bundleName, ctx.acquiredBundleNames);
        if (bundle == null)
        {
            BundleAssetLoadHelper.ReleaseBundles(ctx.acquiredBundleNames);
            return null;
        }

        return BundleAssetLoadHelper.LoadFromBundle(bundle, ctx.assetName, ctx.assetType, ctx.assetPath);
    }

    public void Release(in AssetReleaseContext ctx)
    {
        BundleAssetLoadHelper.ReleaseBundles(ctx.acquiredBundleNames);
    }
}
