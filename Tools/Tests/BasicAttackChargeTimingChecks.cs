using System;
using Spellstrike.Vfx;

// Run with Tools/Test-BasicChargeFade.ps1. Pure timeline checks; no Unity runtime claims.
internal static class BasicAttackChargeTimingChecks
{
    private const float Release = 25f / 30f;
    private const float Fade = BasicAttackChargeTiming.DefaultFadeSeconds;
    private static int assertions;

    private static void Require(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }

    private static void Near(float actual, float expected, string message)
        => Require(Math.Abs(actual - expected) < 0.00002f, message + $": {actual} != {expected}");

    private static void Main()
    {
        var start = BasicAttackChargeTiming.Evaluate(0f, Release, Fade);
        Near(start.Charge01, 0f, "Start progress");
        Near(start.Visibility, 1f, "Pre-release visibility does not suppress the existing faint buildup");
        var halfCharge = BasicAttackChargeTiming.Evaluate(Release / 2f, Release, Fade);
        Near(halfCharge.Charge01, 0.5f, "Buildup progress");
        Near(halfCharge.Visibility, 1f, "Buildup envelope");

        var release = BasicAttackChargeTiming.Evaluate(Release, Release, Fade);
        Near(release.Charge01, 1f, "Release holds full progress");
        Near(release.Visibility, 1f, "No abrupt cut on release");
        var halfFade = BasicAttackChargeTiming.Evaluate(Release + Fade / 2f, Release, Fade);
        Near(halfFade.Charge01, 1f, "No reverse charging during fade");
        Near(halfFade.Visibility, 0.5f, "Half fade");
        Require(!BasicAttackChargeTiming.Evaluate(Release + Fade, Release, Fade).IsVisible, "Exact fade endpoint hides");
        Require(!BasicAttackChargeTiming.Evaluate(Release + 2f, Release, Fade).IsVisible, "Recovery never restarts charge");

        foreach (int fps in new[] { 15, 30, 60, 120 })
        {
            float lastVisibility = 1f;
            for (int frame = 0; frame <= fps * 2; frame++)
            {
                float t = (float)frame / fps;
                var sample = BasicAttackChargeTiming.Evaluate(t, Release, Fade);
                Require(sample.Charge01 >= 0f && sample.Charge01 <= 1f, "Bounded progress");
                Require(sample.Visibility >= 0f && sample.Visibility <= 1f, "Bounded intensity");
                if (t < Release) Near(sample.Visibility, 1f, "No premature fading");
                else
                {
                    Near(sample.Charge01, 1f, "Peak progress held throughout recovery");
                    Require(sample.Visibility <= lastVisibility + 0.00002f, "Fade must be monotonic");
                }
                lastVisibility = sample.Visibility;
            }
        }

        var paused = BasicAttackChargeTiming.Evaluate(Release + Fade / 2f, Release, Fade);
        Near(paused.Visibility, halfFade.Visibility, "Unchanged animation time holds fade");
        var nextCast = BasicAttackChargeTiming.Evaluate(0f, Release, Fade);
        Near(nextCast.Visibility, 1f, "New cast restores visibility");
        Near(nextCast.Charge01, 0f, "New cast starts at zero progress");
        Require(!BasicAttackChargeTiming.Evaluate(Release, Release, 0f).IsVisible, "Zero fade restores immediate hide");
        Require(BasicAttackChargeTiming.Evaluate(Release - 0.01f, Release, 0f).IsVisible, "Zero fade preserves buildup");
        Require(!BasicAttackChargeTiming.Evaluate(-1f, Release, Fade).IsVisible, "Negative time hides");
        Require(!BasicAttackChargeTiming.Evaluate(float.NaN, Release, Fade).IsVisible, "NaN time hides");
        Require(!BasicAttackChargeTiming.Evaluate(1f, 0f, Fade).IsVisible, "Invalid release hides");
        Require(!BasicAttackChargeTiming.Evaluate(Release, Release, float.PositiveInfinity).IsVisible, "Invalid duration hides");

        Console.WriteLine($"PASS: {assertions} timing assertions; 15/30/60/120 fps samples, release continuity, monotonic fade, endpoint, pause, restart and zero-duration behavior.");
    }
}
