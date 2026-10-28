namespace Spellstrike.Vfx
{
    /// <summary>Pure presentation timeline shared by the animation binding and isolated preview.</summary>
    public static class BasicAttackChargeTiming
    {
        public const float DefaultFadeSeconds = 0.16f;

        public readonly struct Sample
        {
            public readonly float Charge01;
            public readonly float Visibility;
            public bool IsVisible => Visibility > 0f;

            public Sample(float charge01, float visibility)
            {
                Charge01 = charge01;
                Visibility = visibility;
            }
        }

        // Seconds are sampled from the animation. Pausing/slowing it also pauses/slows the fade.
        public static Sample Evaluate(float seconds, float releaseSeconds, float fadeSeconds)
        {
            if (!IsFinite(seconds) || !IsFinite(releaseSeconds) || !IsFinite(fadeSeconds) ||
                seconds < 0f || releaseSeconds <= 0f)
                return new Sample(0f, 0f);

            if (seconds < releaseSeconds)
                return new Sample(seconds / releaseSeconds, 1f);

            float elapsed = seconds - releaseSeconds;
            if (fadeSeconds <= 0f || seconds >= releaseSeconds + fadeSeconds)
                return new Sample(1f, 0f);

            float t = elapsed / fadeSeconds;
            float smoothFade = 1f - t * t * (3f - 2f * t);
            return new Sample(1f, smoothFade);
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
