using CountersPlus.ConfigModels;
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using Zenject;

namespace CountersPlus.Counters
{
    internal class FailCounter : Counter<FailConfigModel>
    {
        private static long difficulty = 0;
        private static int restarts = 0;

        [Inject] private GameEnergyCounter energyCounter { get; set; }
        [Inject] private PlayerDataModel playerData { get; set; }
        [Inject] private BeatmapLevel beatmap { get; set; }
        [Inject] private BeatmapKey beatmapKey { get; set; }
        private int count = 0;
        private TMP_Text counter;
        private Utils.SharedCoroutineStarter coroutineStarter;
        private Coroutine colorAnimation;

        public override void CounterInit()
        {
            long currentHash = LongExponent(beatmap.levelID.ToCharArray().Sum(x => (long)x), (int)beatmapKey.difficulty);
            if (Settings.ShowRestartsInstead)
            {
                if (difficulty == currentHash)
                {
                    restarts++;
                    count = restarts;
                }
                else
                {
                    restarts = count = 0;
                    difficulty = currentHash;
                }
            }
            else
            {
                count = playerData.playerData.playerAllOverallStatsData.allOverallStatsData.failedLevelsCount;
                energyCounter.gameEnergyDidReach0Event += SlowlyAnnoyThePlayerBecauseTheyFailed;
            }
            GenerateBasicText($"{(Settings.ShowRestartsInstead ? "Restarts" : "Fails")}", out counter);
            counter.text = count.ToString();
        }

        public override void CounterDestroy()
        {
            energyCounter.gameEnergyDidReach0Event -= SlowlyAnnoyThePlayerBecauseTheyFailed;
            if (coroutineStarter != null && colorAnimation != null)
                coroutineStarter.StopCoroutine(colorAnimation);
            colorAnimation = null;
        }

        private void SlowlyAnnoyThePlayerBecauseTheyFailed()
        {
            counter.text = (count + 1).ToString();
            coroutineStarter = Utils.SharedCoroutineStarter.instance;
            if (colorAnimation != null) coroutineStarter.StopCoroutine(colorAnimation);
            colorAnimation = coroutineStarter.StartCoroutine(ChangeTextColorToAnnoyThePlayerEvenMore());
        }

        private IEnumerator ChangeTextColorToAnnoyThePlayerEvenMore()
        {
            float t = 0;
            var wait = new WaitForEndOfFrame();
            while (t <= 1)
            {
                yield return wait;
                t += Time.deltaTime;
                counter.color = Color.Lerp(Color.white, Color.red, t);
            }
            counter.color = Color.red;
            colorAnimation = null;
        }

        private long LongExponent(long x, int pow)
        {
            string binary = Convert.ToString(pow, 2);

            int[] arr = new int[binary.Length];
            int i = 0;
            foreach (var ch in binary)
            {
                arr[i++] = Convert.ToInt32(ch.ToString());
            }

            // We use a nifty trick to calculate exponent in as little as 2 long2(n) multiplications.
            long res = x;
            for (int j = 1; j < arr.Length; j++)
            {
                switch (arr[j])
                {
                    case 0:
                        res *= res;
                        break;
                    case 1:
                        res *= res;
                        res *= x;
                        break;
                }
            }

            return res;
        }
    }
}
