using Hive.Versioning;
using IPA.Loader;
using System.Collections;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace CountersPlus.Utils
{
    public class VersionUtility
    {
        public Version PluginVersion { get; private set; } = new Version("0.0.0");
        public Version BeatModsVersion { get; private set; } = new Version("0.0.0");
        public bool HasLatestVersion => PluginVersion >= BeatModsVersion;

        public VersionUtility()
        {
            // I could grab this straight from PluginMetadata but this is for cleanness.
            PluginVersion = PluginManager.GetPlugin("Counters+").HVersion;

            SharedCoroutineStarter.instance.StartCoroutine(GetBeatModsVersion());
        }

        private IEnumerator GetBeatModsVersion()
        {
            using (UnityWebRequest www = UnityWebRequest.Get("https://beatmods.com/api/mods/37"))
            {
                www.SetRequestHeader("User-Agent", $"Counters+/{PluginVersion}");
                yield return www.SendWebRequest();
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Plugin.Logger.Error("Failed to download version info.");
                    yield break;
                }
                BeatmodsResult result = JsonConvert.DeserializeObject<BeatmodsResult>(www.downloadHandler.text);
                foreach (BeatmodsModVersionResult versionResult in result.mod.versions)
                {
                    if (versionResult.status != "verified") continue;
                    BeatModsVersion = new Version(versionResult.modVersion);
                    break;
                }
            }
            if (!HasLatestVersion) Plugin.Logger.Warn("Uh oh! We aren't up to date!");
        }


    }

    class BeatmodsResult
    {
        [JsonProperty("mod")] internal BeatmodsModResult mod;
    }

    class BeatmodsModResult
    {
        [JsonProperty("versions")] internal BeatmodsModVersionResult[] versions;
    }

    class BeatmodsModVersionResult
    {
        [JsonProperty("status")] internal string status;
        [JsonProperty("modVersion")] internal string modVersion;
    }
}
