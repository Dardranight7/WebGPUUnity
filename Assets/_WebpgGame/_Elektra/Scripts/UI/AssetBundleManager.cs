using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AssetBundleManager
{
    private static Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();

    public static bool IsLoaded(string path) => loadedBundles.ContainsKey(path);

    public static AssetBundle GetBundle(string path)
    {
        loadedBundles.TryGetValue(path, out var bundle);
        return bundle;
    }

    // COROUTINE VERSION — MAIN THREAD SAFE
    public static IEnumerator LoadBundleCoroutine(string path, System.Action<AssetBundle> onComplete)
    {
        // Ya cargado → retornamos inmediatamente
        if (loadedBundles.TryGetValue(path, out var existing))
        {
            onComplete?.Invoke(existing);
            yield break;
        }

        // Cargar AssetBundle desde archivo
        var request = AssetBundle.LoadFromFileAsync(path);
        yield return request;

        var bundle = request.assetBundle;

        if (bundle == null)
        {
            Debug.LogError("Falló la carga del AssetBundle: " + path);
            onComplete?.Invoke(null);
            yield break;
        }

        loadedBundles[path] = bundle;
        onComplete?.Invoke(bundle);
    }
}
