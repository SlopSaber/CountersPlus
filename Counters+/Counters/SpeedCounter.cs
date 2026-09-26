using CountersPlus.ConfigModels;
using TMPro;
using UnityEngine;
using Zenject;

namespace CountersPlus.Counters
{
    internal class SpeedCounter : Counter<SpeedConfigModel>, ITickable
    {
        [Inject] private SaberManager saberManager { get; set; }

        private Saber right;
        private Saber left;
        private double rightSpeedTotal;
        private double leftSpeedTotal;
        private long rightSampleCount;
        private long leftSampleCount;
        private float fastestSpeed = float.NaN;
        private TMP_Text averageCounter;
        private TMP_Text fastestCounter;

        private float t;

        public override void CounterInit()
        {
            right = saberManager.rightSaber;
            left = saberManager.leftSaber;

            switch (Settings.Mode)
            {
                case SpeedMode.Average:
                case SpeedMode.SplitAverage:
                    GenerateBasicText("Average Speed", out averageCounter);
                    break;
                case SpeedMode.Top5Sec:
                    GenerateBasicText("Recent Top Speed", out fastestCounter);
                    break;
                case SpeedMode.Both:
                case SpeedMode.SplitBoth:
                    GenerateBasicText("Average Speed", out averageCounter);
                    var label = CanvasUtility.CreateTextFromSettings(Settings, new Vector3(0, -1, 0));
                    label.fontSize = 3;
                    label.text = "Recent Top Speed";
                    fastestCounter = CanvasUtility.CreateTextFromSettings(Settings, new Vector3(0, -1.4f, 0));
                    fastestCounter.text = "0";
                    break;
            }
        }

        public void Tick()
        {
            int precision = Settings.DecimalPrecision;
            // YES I AM USING GOTO TO LIMIT CODE DUPLICATION NOW STOP FLAMING ME
            switch (Settings.Mode)
            {
                case SpeedMode.Top5Sec:
                    TickFastestSpeed();
                    break;

                case SpeedMode.Both:
                    TickFastestSpeed();
                    goto case SpeedMode.Average;
                case SpeedMode.Average:
                    rightSpeedTotal += (right.bladeSpeed + left.bladeSpeed) / 2f;
                    rightSampleCount++;
                    averageCounter.text = ((float)(rightSpeedTotal / rightSampleCount)).ToString($"F{precision}");
                    break;

                case SpeedMode.SplitBoth:
                    TickFastestSpeed();
                    goto case SpeedMode.SplitAverage;
                case SpeedMode.SplitAverage:
                    rightSpeedTotal += right.bladeSpeed;
                    leftSpeedTotal += left.bladeSpeed;
                    rightSampleCount++;
                    leftSampleCount++;
                    averageCounter.text = $"{((float)(leftSpeedTotal / leftSampleCount)).ToString($"F{precision}")} | {((float)(rightSpeedTotal / rightSampleCount)).ToString($"F{precision}")}";
                    break;
            }
        }

        // Ticked function instead of IEnumerator because its legit just better
        private void TickFastestSpeed()
        {
            float speed = (right.bladeSpeed + left.bladeSpeed) / 2f;
            if (speed > fastestSpeed || float.IsNaN(fastestSpeed)) fastestSpeed = speed;
            t += Time.deltaTime;
            if (t >= 5)
            {
                t = 0;
                fastestCounter.text = fastestSpeed.ToString($"F{Settings.DecimalPrecision}");
                fastestSpeed = float.NaN;
            }
        }
    }
}
