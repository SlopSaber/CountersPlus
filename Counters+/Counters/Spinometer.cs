using CountersPlus.ConfigModels;
using System.Collections;
using TMPro;
using UnityEngine;
using Zenject;

namespace CountersPlus.Counters
{
    internal class Spinometer : Counter<SpinometerConfigModel>, ITickable
    {
        [Inject] private SaberManager saberManager { get; set; }

        private Saber leftSaber = null;
        private Saber rightSaber = null;
        private double rightAngleTotal;
        private double leftAngleTotal;
        private Quaternion previousRightRotation;
        private Quaternion previousLeftRotation;
        private bool hasPreviousRotation;
        private Utils.SharedCoroutineStarter coroutineStarter;
        private Coroutine secondTick;
        private float highestSpin;
        private TMP_Text spinometer;

        public override void CounterInit()
        {
            leftSaber = saberManager.leftSaber;
            rightSaber = saberManager.rightSaber;
            GenerateBasicText("Spinometer", out spinometer);
            coroutineStarter = Utils.SharedCoroutineStarter.instance;
            secondTick = coroutineStarter.StartCoroutine(SecondTick());
        }

        public override void CounterDestroy()
        {
            if (coroutineStarter != null && secondTick != null)
                coroutineStarter.StopCoroutine(secondTick);
            secondTick = null;
        }

        public void Tick()
        {
            if (leftSaber != saberManager.leftSaber)
            {
                leftSaber = saberManager.leftSaber;
            }
            if (rightSaber != saberManager.rightSaber)
            {
                rightSaber = saberManager.rightSaber;
            }
            Quaternion leftRotation = leftSaber.transform.rotation;
            Quaternion rightRotation = rightSaber.transform.rotation;
            if (hasPreviousRotation)
            {
                leftAngleTotal += Quaternion.Angle(leftRotation, previousLeftRotation);
                rightAngleTotal += Quaternion.Angle(rightRotation, previousRightRotation);
            }
            previousLeftRotation = leftRotation;
            previousRightRotation = rightRotation;
            hasPreviousRotation = true;
        }

        private IEnumerator SecondTick()
        {
            var wait = new WaitForSecondsRealtime(1);
            while (true)
            {
                yield return wait;
                hasPreviousRotation = false;
                float leftSpeed = (float)leftAngleTotal;
                float rightSpeed = (float)rightAngleTotal;
                leftAngleTotal = 0;
                rightAngleTotal = 0;
                float averageSpeed = (leftSpeed + rightSpeed) / 2;
                if (leftSpeed > highestSpin) highestSpin = leftSpeed;
                if (rightSpeed > highestSpin) highestSpin = rightSpeed;

                switch (Settings.Mode)
                {
                    case SpinometerMode.Average:
                        spinometer.text = $"<color=#{DetermineColor(averageSpeed)}>{Mathf.RoundToInt(averageSpeed)}</color>";
                        break;
                    case SpinometerMode.Highest:
                        spinometer.text = $"<color=#{DetermineColor(highestSpin)}>{Mathf.RoundToInt(highestSpin)}</color>";
                        break;
                    case SpinometerMode.SplitAverage:
                        spinometer.text = $"<color=#{DetermineColor(leftSpeed)}>{Mathf.RoundToInt(leftSpeed)}</color> | <color=#{DetermineColor(rightSpeed)}>{Mathf.RoundToInt(rightSpeed)}</color>";       
                        break;
                }
            }
        }

        private string DetermineColor(float speed)
        {
            ColorUtility.TryParseHtmlString("#FFA500", out Color orange);
            Color color = Color.Lerp(Color.white, orange, speed / 3600);
            if (speed >= 3600) color = Color.red;
            return ColorUtility.ToHtmlStringRGB(color);
        }
    }
}
