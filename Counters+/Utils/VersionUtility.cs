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
            using (UnityWebRequest www = UnityWebRequest.Get("https://beatmods.com/api/v3/mods/37"))
            {
                www.SetRequestHeader("User-Agent", $"Counters+/{PluginVersion}");
                yield return www.SendWebRequest();
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Plugin.Logger.Warn($"Version check unavailable: HTTP {www.responseCode}, {www.error}");
                    yield break;
                }
                BeatmodsResult result = JsonConvert.DeserializeObject<BeatmodsResult>(www.downloadHandler.text);
                foreach (BeatmodsModVersionResult versionResult in result.versions)
                {
                    if (versionResult.status != "verified") continue;
                    Version version = new Version(versionResult.modVersion);
                    if (version > BeatModsVersion) BeatModsVersion = version;
                }
            }
            if (!HasLatestVersion) Plugin.Logger.Warn("Uh oh! We aren't up to date!");
        }


    }

    class BeatmodsResult
    {
        [JsonProperty("versions")] internal BeatmodsModVersionResult[] versions;
    }

    class BeatmodsModVersionResult
    {
        [JsonProperty("status")] internal string status;
        [JsonProperty("semver")] internal string modVersion;
    }
}
