using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Heck;
using Heck.Animation;
using UnityEngine;
using Zenject;

namespace CountersPlus.Utils
{
    // Keeps gameplay canvases in player space, including lanes rotated by Noodle note worldRotation.
    [DefaultExecutionOrder(1000)]
    internal sealed class GameplayHUDMotionController : MonoBehaviour
    {
        private struct Pose
        {
            internal Vector3 Position;
            internal Quaternion Rotation;

            internal Pose(Transform transform)
            {
                Position = transform.position;
                Rotation = transform.rotation;
            }
        }

        private struct LaneSample
        {
            internal float Time;
            internal Quaternion BaseRotation;
            internal string[] Tracks;

            internal LaneSample(float time, Quaternion baseRotation, string[] tracks)
            {
                Time = time;
                BaseRotation = baseRotation;
                Tracks = tracks;
            }
        }

        private readonly Dictionary<Canvas, Pose> _staticCanvases = new Dictionary<Canvas, Pose>();
        private readonly List<LaneSample> _laneSamples = new List<LaneSample>();

        private CanvasUtility _canvasUtility;
        private AudioTimeSyncController _audioTimeSyncController;
        private PlayerTransforms _playerTransforms;
        private Dictionary<string, Track> _tracks;
        private bool _leftHanded;
        private Transform _playerOrigin;
        private Pose _initialPlayerPose;

        [Inject]
        private void Construct(CanvasUtility canvasUtility, IReadonlyBeatmapData beatmapData,
            AudioTimeSyncController audioTimeSyncController, PlayerTransforms playerTransforms,
            GameplayCoreSceneSetupData sceneData, [InjectOptional] Dictionary<string, Track> tracks)
        {
            _canvasUtility = canvasUtility;
            _audioTimeSyncController = audioTimeSyncController;
            _playerTransforms = playerTransforms;
            _tracks = tracks;
            _leftHanded = sceneData.playerSpecificSettings.leftHanded;

            if (sceneData.beatmapKey.characteristic != BeatmapCharacteristic.Degree90 &&
                sceneData.beatmapKey.characteristic != BeatmapCharacteristic.Degree360)
            {
                foreach (NoteData noteData in OrderNotes(beatmapData.GetBeatmapDataItems<NoteData>(0)))
                {
                    if (noteData.gameplayType == NoteData.GameplayType.Bomb ||
                        (_laneSamples.Count > 0 && Mathf.Approximately(_laneSamples[_laneSamples.Count - 1].Time, noteData.time)))
                    {
                        continue;
                    }

                    IDictionary<string, object> customData = noteData.GetType()
                        .GetProperty("customData")?.GetValue(noteData) as IDictionary<string, object>;
                    _laneSamples.Add(new LaneSample(noteData.time, ReadRotation(customData), ReadTracks(customData)));
                }
            }
        }

        private void Start()
        {
            // Noodle inserts its root above the origin transform during scene initialization.
            _playerOrigin = _playerTransforms._originTransform.parent;
            _initialPlayerPose = new Pose(_playerOrigin);
        }

        private Quaternion ReadRotation(IDictionary<string, object> customData)
        {
            if (customData == null ||
                (!customData.TryGetValue("worldRotation", out object value) &&
                 !customData.TryGetValue("_rotation", out value)) || value == null)
            {
                return Quaternion.identity;
            }

            Quaternion rotation;
            if (value is IList angles && angles.Count >= 3)
            {
                rotation = Quaternion.Euler(Convert.ToSingle(angles[0]), Convert.ToSingle(angles[1]),
                    Convert.ToSingle(angles[2]));
            }
            else
            {
                rotation = Quaternion.Euler(0f, Convert.ToSingle(value), 0f);
            }

            return rotation.Mirror(_leftHanded);
        }

        private static string[] ReadTracks(IDictionary<string, object> customData)
        {
            if (customData == null ||
                (!customData.TryGetValue("track", out object value) &&
                 !customData.TryGetValue("_track", out value)) || value == null)
            {
                return Array.Empty<string>();
            }

            if (value is string name) return new[] { name };
            if (value is IEnumerable names) return names.Cast<object>().OfType<string>().ToArray();
            return Array.Empty<string>();
        }

        private void LateUpdate()
        {
            if (_playerOrigin == null) return;

            Pose playerPose = new Pose(_playerOrigin);
            Quaternion playerDelta = playerPose.Rotation * Quaternion.Inverse(_initialPlayerPose.Rotation);
            Quaternion laneRotation = CurrentLaneRotation();

            foreach (Canvas canvas in _canvasUtility.Canvases)
            {
                if (canvas == null) continue;

                Transform transform = canvas.transform;
                Pose basePose;
                SoftParent softParent = canvas.GetComponent<SoftParent>();
                if (softParent != null)
                {
                    // SoftParent restores base HUD pose in Update before this component runs.
                    basePose = new Pose(transform);
                }
                else if (!_staticCanvases.TryGetValue(canvas, out basePose))
                {
                    basePose = new Pose(transform);
                    _staticCanvases.Add(canvas, basePose);
                }

                // A HUD already inside the moving player hierarchy needs no second player delta.
                bool followsPlayer = softParent != null && softParent.Parent != null &&
                    softParent.Parent.IsChildOf(_playerOrigin);
                Vector3 position = followsPlayer
                    ? basePose.Position
                    : playerPose.Position + playerDelta * (basePose.Position - _initialPlayerPose.Position);
                Quaternion rotation = followsPlayer ? basePose.Rotation : playerDelta * basePose.Rotation;
                position = playerPose.Position + laneRotation * (position - playerPose.Position);
                rotation = laneRotation * rotation;
                transform.SetPositionAndRotation(position, rotation);
            }
        }

        private Quaternion CurrentLaneRotation()
        {
            if (_laneSamples.Count == 0) return Quaternion.identity;

            float songTime = _audioTimeSyncController.songTime;
            int low = 0;
            int high = _laneSamples.Count - 1;
            int current = -1;
            while (low <= high)
            {
                int middle = low + ((high - low) / 2);
                if (_laneSamples[middle].Time <= songTime)
                {
                    current = middle;
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            if (current < 0) return SampleRotation(_laneSamples[0]);
            LaneSample from = _laneSamples[current];
            Quaternion fromRotation = SampleRotation(from);
            if (current == _laneSamples.Count - 1) return fromRotation;

            LaneSample to = _laneSamples[current + 1];
            if (from.Tracks.Length > 0 && SameTracks(from.Tracks, to.Tracks)) return fromRotation;

            float progress = Mathf.InverseLerp(from.Time, to.Time, songTime);
            return Quaternion.Slerp(fromRotation, SampleRotation(to), progress);
        }

        private Quaternion SampleRotation(LaneSample sample)
        {
            Quaternion rotation = sample.BaseRotation;
            if (_tracks == null) return rotation;
            foreach (string name in sample.Tracks)
            {
                if (_tracks.TryGetValue(name, out Track track))
                {
                    rotation *= (track.GetProperty<Quaternion>("offsetWorldRotation") ?? Quaternion.identity)
                        .Mirror(_leftHanded);
                }
            }

            return rotation;
        }

        private static bool SameTracks(string[] first, string[] second)
        {
            if (first.Length != second.Length) return false;
            for (int i = 0; i < first.Length; i++)
            {
                if (first[i] != second[i]) return false;
            }

            return true;
        }

        private static IEnumerable<NoteData> OrderNotes(IEnumerable<NoteData> source)
        {
            if (Thread.CurrentThread.IsThreadPoolThread) return source.OrderBy(note => note.time);

            NoteData[] notes = source.ToArray();
            if (notes.Length < 128) return notes.OrderBy(note => note.time);

            // Buffer and read sort keys before custom-data callbacks can change note times.
            var times = new float[notes.Length];
            for (int index = 0; index < notes.Length; index++) times[index] = notes[index].time;

            Task<int[]> task;
            if (ExecutionContext.IsFlowSuppressed())
            {
                task = QueueNoteOrder(times);
            }
            else
            {
                using (ExecutionContext.SuppressFlow())
                {
                    task = QueueNoteOrder(times);
                }
            }

            return OrderedNotes(notes, task.GetAwaiter().GetResult());
        }

        private static Task<int[]> QueueNoteOrder(float[] times)
        {
            return Task.Factory.StartNew(
                ComputeOwnedNoteOrder,
                times,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);
        }

        private static int[] ComputeOwnedNoteOrder(object state)
        {
            var times = (float[])state;
            var order = new int[times.Length];
            for (int index = 0; index < order.Length; index++) order[index] = index;
            Array.Sort(order, new NoteTimeComparer(times));
            return order;
        }

        private static IEnumerable<NoteData> OrderedNotes(NoteData[] notes, int[] order)
        {
            foreach (int index in order) yield return notes[index];
        }

        private sealed class NoteTimeComparer : IComparer<int>
        {
            private readonly float[] _times;

            internal NoteTimeComparer(float[] times)
            {
                _times = times;
            }

            public int Compare(int first, int second)
            {
                int comparison = _times[first].CompareTo(_times[second]);
                return comparison == 0 ? first.CompareTo(second) : comparison;
            }
        }
    }
}
