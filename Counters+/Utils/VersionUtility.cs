using Hive.Versioning;
using IPA.Loader;
using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Version = Hive.Versioning.Version;

namespace CountersPlus.Utils
{
    public class VersionUtility : IDisposable
    {
        private CancellationTokenSource? cancellation = new CancellationTokenSource();
        private UnityWebRequest? download;
        private Task<VersionResult>? parseTask;
        private bool disposed;

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
            CancellationToken token = cancellation!.Token;
            try
            {
                string response;
                using (UnityWebRequest www = UnityWebRequest.Get("https://beatmods.com/api/mods/37"))
                {
                    download = www;
                    www.SetRequestHeader("User-Agent", $"Counters+/{PluginVersion}");
                    yield return www.SendWebRequest();
                    download = null;
                    if (disposed) yield break;
                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Plugin.Logger.Error("Failed to download version info.");
                        yield break;
                    }
                    response = www.downloadHandler.text;
                }

                VersionResult latestVersion;
                if (JsonConvert.DefaultSettings != null)
                {
                    // Global serializers can call custom converters and callbacks on the owner.
                    latestVersion = FindVerifiedVersion(JsonConvert.DeserializeObject<BeatmodsResult>(response), token);
                }
                else
                {
                    var request = new VersionRequest(response, token);
                    parseTask = Task.Run(() => ParseVersion(request), token);
                    while (!parseTask.IsCompleted) yield return null;
                    if (disposed)
                    {
                        if (parseTask.IsFaulted) _ = parseTask.Exception;
                        yield break;
                    }
                    latestVersion = parseTask.GetAwaiter().GetResult();
                }

                if (disposed) yield break;
                if (latestVersion.Found) BeatModsVersion = new Version(latestVersion.Value!);
                if (!HasLatestVersion) Plugin.Logger.Warn("Uh oh! We aren't up to date!");
            }
            finally
            {
                download = null;
                // Cancellation retires publication; the physical task still owns its request.
                if (parseTask == null || parseTask.IsCompleted)
                {
                    parseTask = null;
                    cancellation?.Dispose();
                    cancellation = null;
                }
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            cancellation?.Cancel();
            download?.Abort();
        }

        private static VersionResult ParseVersion(VersionRequest request)
        {
            request.Token.ThrowIfCancellationRequested();
            // Match JsonConvert's default behavior without invoking its global settings factory.
            JsonSerializer serializer = JsonSerializer.Create();
            serializer.CheckAdditionalContent = true;
            using (var text = new StringReader(request.Response))
            using (var reader = new JsonTextReader(text))
            {
                return FindVerifiedVersion(serializer.Deserialize<BeatmodsResult>(reader), request.Token);
            }
        }

        private static VersionResult FindVerifiedVersion(BeatmodsResult result, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            foreach (BeatmodsModVersionResult versionResult in result.mod.versions)
            {
                token.ThrowIfCancellationRequested();
                if (versionResult.status == "verified") return new VersionResult(versionResult.modVersion);
            }
            return default;
        }

        private readonly struct VersionRequest
        {
            public readonly string Response;
            public readonly CancellationToken Token;

            public VersionRequest(string response, CancellationToken token)
            {
                Response = response;
                Token = token;
            }
        }

        private readonly struct VersionResult
        {
            public readonly bool Found;
            public readonly string? Value;

            public VersionResult(string? value)
            {
                Found = true;
                Value = value;
            }
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
