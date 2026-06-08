using System;

[Serializable]
public class AssetCatalog
{
    public string version;
    public int buildNumber;
    public string platform;
    public string buildMode;
    public string packingRule;
    public string bundleRoot;
    public AssetCatalogEntry[] entries;
}
