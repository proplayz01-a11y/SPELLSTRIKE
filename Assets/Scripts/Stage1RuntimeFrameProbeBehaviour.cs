#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

public sealed class Stage1RuntimeFrameProbeBehaviour : MonoBehaviour
{
    public static readonly List<double> FrameMilliseconds = new List<double>(1024);
    public static int RenderedFrames { get; private set; }

    private bool previousRunInBackground;

    public static void ResetCapture()
    {
        FrameMilliseconds.Clear();
        RenderedFrames = 0;
    }

    private void Awake()
    {
        previousRunInBackground = Application.runInBackground;
        Application.runInBackground = true;
    }

    private void Update()
    {
        double milliseconds = Time.unscaledDeltaTime * 1000.0;
        if (milliseconds > 0.000001 && milliseconds < 1000.0)
            FrameMilliseconds.Add(milliseconds);

        RenderedFrames++;
    }

    private void OnDestroy()
    {
        Application.runInBackground = previousRunInBackground;
    }
}
#endif
