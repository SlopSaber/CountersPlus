using CountersPlus.ConfigModels;
using CountersPlus.Counters.Interfaces;
using CountersPlus.Counters.NoteCountProcessors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Zenject;

namespace CountersPlus.Counters
{
    internal class PBCounter : Counter<PBConfigModel>, IScoreEventHandler
    {
        private readonly Vector3 SCORE_COUNTER_OFFSET = new Vector3(0, -1.85f, 0); 

        [Inject] private GameplayCoreSceneSetupData data { get; set; }
        [Inject] private PlayerDataModel playerDataModel { get; set; }
        [Inject] private ScoreConfigModel scoreConfig { get; set; }
        [Inject] private RelativeScoreAndImmediateRankCounter relativeScoreAndImmediateRank { get; set; }
        [Inject] private IReadonlyBeatmapData beatmapData { get; set; }

        private TMP_Text counter;
        private PlayerLevelStatsData stats;

        private Color white = Color.white; // Caching it beforehand, since calling the constant makes a new struct

        private int maxPossibleScore = 0;
        private int highScore;
        private float pbRatio;

        public override void CounterInit()
        {
            BeatmapBasicData beatmap = data.beatmapBasicData;
            
            maxPossibleScore = ComputeMaximumScore(beatmapData);

            stats = playerDataModel.playerData.GetOrCreatePlayerLevelStatsData(data.beatmapKey);
            highScore = stats.highScore;

            if (scoreConfig.Enabled && Settings.UnderScore)
            {
                HUDCanvas scoreCanvas = CanvasUtility.GetCanvasSettingsFromID(scoreConfig.CanvasID);
                counter = CanvasUtility.CreateTextFromSettings(scoreConfig, SCORE_COUNTER_OFFSET * (3f / scoreCanvas.PositionScale));
            }
            else
            {
                counter = CanvasUtility.CreateTextFromSettings(Settings);
            }
            counter.alignment = TextAlignmentOptions.Top;
            counter.fontSize = Settings.TextSize;

            pbRatio = (float)highScore / maxPossibleScore;

            SetPersonalBest(pbRatio);
            ScoreUpdated(0);
        }

        public void MaxScoreUpdated(int maxModifiedScore) { }

        public void ScoreUpdated(int modifiedScore)
        {
            if (maxPossibleScore != 0 && modifiedScore > highScore)
            {
                SetPersonalBest(modifiedScore / (float)maxPossibleScore);
            }

            counter.color = Settings.Mode switch
            {
                // Show default color when setting the first PB and setting is enabled
                _ when Settings.HideFirstScore && stats.highScore == 0 => Settings.DefaultColor,

                // Relative % is above PB %
                PBMode.Relative when relativeScoreAndImmediateRank.relativeScore >= pbRatio
                    => Settings.BetterColor,

                // Relative % is approaching PB %
                PBMode.Relative when relativeScoreAndImmediateRank.relativeScore < pbRatio
                    => Color.Lerp(white, Settings.DefaultColor, relativeScoreAndImmediateRank.relativeScore / pbRatio),

                // New high score, show PB color
                PBMode.Absolute when modifiedScore >= highScore
                    => Settings.BetterColor,

                // Current score is approaching high school
                PBMode.Absolute when modifiedScore < highScore
                    => Color.Lerp(white, Settings.DefaultColor, modifiedScore / (float)highScore),

                // The "C# stop yelling at me" case
                _ => Settings.DefaultColor,
            };
        }

        private void SetPersonalBest(float pb)
        {
            if (Settings.HideFirstScore && stats.highScore == 0) counter.text = "PB: --";
            else counter.text = $"PB: {(pb * 100).ToString($"F{Settings.DecimalPrecision}")}%";
        }

        private static int ComputeMaximumScore(IReadonlyBeatmapData beatmap)
        {
            if (Thread.CurrentThread.IsThreadPoolThread || HasPatchedScoreCalculation())
            {
                return ScoreModel.ComputeMaxMultipliedScoreForBeatmap(beatmap);
            }

            RuntimeHelpers.RunClassConstructor(typeof(ScoreModel).TypeHandle);
            var notes = beatmap.GetBeatmapDataItems<NoteData>(0);
            var sliders = beatmap.GetBeatmapDataItems<SliderData>(0);
            var captured = new List<CapturedScoreInput>(1000);

            foreach (var note in notes)
            {
                if ((int)note.scoringType == -1 || (int)note.scoringType == 0) continue;
                var scoringType = note.scoringType;
                float time = note.time;
                captured.Add(new CapturedScoreInput(time, ScoreModel.GetNoteScoreDefinition(scoringType)));
            }

            foreach (var slider in sliders)
            {
                if ((int)slider.sliderType != 1) continue;
                for (int slice = 1; slice < slider.sliceCount; slice++)
                {
                    float t = (float)slice / (slider.sliceCount - 1);
                    float time = Mathf.LerpUnclamped(slider.time, slider.tailTime, t);
                    captured.Add(new CapturedScoreInput(time, ScoreModel.GetNoteScoreDefinition((NoteData.ScoringType)5)));
                }
            }

            // Enumerators may change score definitions, so copy scores after both finish.
            var owned = new OwnedScoreInput[captured.Count];
            for (int index = 0; index < captured.Count; index++)
            {
                var input = captured[index];
                owned[index] = new OwnedScoreInput(input.Time, input.Definition.maxCutScore);
            }

            if (owned.Length < 128) return ComputeOwnedMaximumScore(owned);

            Task<int> task;
            if (ExecutionContext.IsFlowSuppressed())
            {
                task = StartScorePreparation(owned);
            }
            else
            {
                using (ExecutionContext.SuppressFlow())
                {
                    task = StartScorePreparation(owned);
                }
            }

            return task.GetAwaiter().GetResult();
        }

        private static Task<int> StartScorePreparation(OwnedScoreInput[] owned)
        {
            return Task.Factory.StartNew(
                static state => ComputeOwnedMaximumScore((OwnedScoreInput[])state!),
                owned,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);
        }

        private static int ComputeOwnedMaximumScore(OwnedScoreInput[] owned)
        {
            Array.Sort(owned);
            int score = 0;
            int multiplier = 1;
            int progress = 0;
            int maxProgress = 2;
            foreach (var input in owned)
            {
                if (multiplier < 8)
                {
                    if (progress < maxProgress) progress++;
                    if (progress >= maxProgress)
                    {
                        multiplier *= 2;
                        progress = 0;
                        maxProgress = multiplier * 2;
                    }
                }

                score = unchecked(score + input.MaxCutScore * multiplier);
            }

            return score;
        }

        private static bool HasPatchedScoreCalculation()
        {
            foreach (var method in global::HarmonyLib.Harmony.GetAllPatchedMethods())
            {
                var type = method.DeclaringType;
                string name = method.Name;
                if (type == typeof(ScoreModel) &&
                    (name == nameof(ScoreModel.ComputeMaxMultipliedScoreForBeatmap) ||
                     name == nameof(ScoreModel.GetNoteScoreDefinition) || name == ".cctor")) return true;
                if (type == typeof(ScoreModel.NoteScoreDefinition) &&
                    (name == "get_maxCutScore" || name == "get_executionOrder" || name == ".ctor")) return true;
                if (type?.FullName == typeof(ScoreModel).FullName + "+MaxScoreCounterElement") return true;
                if (type == typeof(ScoreMultiplierCounter) &&
                    (name == nameof(ScoreMultiplierCounter.Reset) ||
                     name == nameof(ScoreMultiplierCounter.ProcessMultiplierEvent) ||
                     name == "get_multiplier" || name == ".ctor")) return true;
            }

            return false;
        }

        private readonly struct CapturedScoreInput
        {
            public float Time { get; }
            public ScoreModel.NoteScoreDefinition Definition { get; }

            public CapturedScoreInput(float time, ScoreModel.NoteScoreDefinition definition)
            {
                Time = time;
                Definition = definition;
            }
        }

        private readonly struct OwnedScoreInput : IComparable<OwnedScoreInput>
        {
            public float Time { get; }
            public int MaxCutScore { get; }

            public OwnedScoreInput(float time, int maxCutScore)
            {
                Time = time;
                MaxCutScore = maxCutScore;
            }

            public int CompareTo(OwnedScoreInput other)
            {
                int comparison = Time.CompareTo(other.Time);
                // Equal-time execution order is the maximum cut score.
                return comparison == 0 ? MaxCutScore.CompareTo(other.MaxCutScore) : comparison;
            }
        }
    }
}
