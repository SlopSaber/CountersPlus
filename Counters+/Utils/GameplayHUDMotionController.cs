using System.Collections.Generic;
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

        private struct LaneRotation
        {
            internal float Time;
            internal Quaternion Rotation;

            internal LaneRotation(float time, Quaternion rotation)
            {
                Time = time;
                Rotation = rotation;
            }
        }

        private readonly Dictionary<Canvas, Pose> _staticCanvases = new Dictionary<Canvas, Pose>();
        private readonly List<LaneRotation> _laneRotations = new List<LaneRotation>();

        private CanvasUtility _canvasUtility;
        private BeatmapObjectManager _beatmapObjectManager;
        private AudioTimeSyncController _audioTimeSyncController;
        private PlayerTransforms _playerTransforms;
        private Transform _playerOrigin;
        private Pose _initialPlayerPose;

        [Inject]
        private void Construct(CanvasUtility canvasUtility, BeatmapObjectManager beatmapObjectManager,
            AudioTimeSyncController audioTimeSyncController, PlayerTransforms playerTransforms,
            GameplayCoreSceneSetupData sceneData)
        {
            _canvasUtility = canvasUtility;
            _beatmapObjectManager = beatmapObjectManager;
            _audioTimeSyncController = audioTimeSyncController;
            _playerTransforms = playerTransforms;

            if (sceneData.beatmapKey.characteristic != BeatmapCharacteristic.Degree90 &&
                sceneData.beatmapKey.characteristic != BeatmapCharacteristic.Degree360)
            {
                _beatmapObjectManager.noteWasSpawnedEvent += NoteWasSpawned;
            }
        }

        private void Start()
        {
            // Noodle inserts its root above the origin transform during scene initialization.
            _playerOrigin = _playerTransforms._originTransform.parent;
            _initialPlayerPose = new Pose(_playerOrigin);

        }

        private void OnDestroy()
        {
            if (_beatmapObjectManager != null)
            {
                _beatmapObjectManager.noteWasSpawnedEvent -= NoteWasSpawned;
            }
        }

        private void NoteWasSpawned(NoteController noteController)
        {
            float time = noteController.noteData.time;
            Quaternion rotation = noteController.worldRotation;

            // Note spawns are normally ordered. Keep practice seeks and reloads ordered too.
            int index = _laneRotations.Count;
            while (index > 0 && _laneRotations[index - 1].Time > time)
            {
                index--;
            }

            if (index > 0 && Mathf.Approximately(_laneRotations[index - 1].Time, time))
            {
                return;
            }

            _laneRotations.Insert(index, new LaneRotation(time, rotation));
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
            float songTime = _audioTimeSyncController.songTime;
            int low = 0;
            int high = _laneRotations.Count - 1;
            int current = -1;
            while (low <= high)
            {
                int middle = low + ((high - low) / 2);
                if (_laneRotations[middle].Time <= songTime)
                {
                    current = middle;
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            return current >= 0 ? _laneRotations[current].Rotation : Quaternion.identity;
        }
    }
}
