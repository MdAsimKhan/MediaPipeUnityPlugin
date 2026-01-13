using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Components.Containers;
using UnityEngine;

namespace Mediapipe.Unity.Sample.HandLandmarkDetection
{
  public class ASLGoodbyeDetector
  {
    enum State
    {
      None,
      OpenDetected
    }

    State state = State.None;
    float timer = 0f;
  long lastTimestamp = -1;

    const float OPEN_THRESHOLD = 0.12f;
    const float CLOSED_THRESHOLD = 0.075f;
    const float MAX_TIME = 1.2f;
    int openFrames = 0;
    const int MIN_OPEN_FRAMES = 5; // ~0.15s at 30fps
    float openReference = -1f;

    public bool Detect(HandLandmarkerResult result, long timestampMillisec)
    {
      if (result.handLandmarks == null || result.handLandmarks.Count == 0)
      {
        Reset();
        return false;
      }

      var lm = result.handLandmarks[0]; // single hand (ASL is one-handed)

      if (!IsPalmFacingCamera(lm))
      {
        Reset();
        return false;
      }

    float openness = HandOpenness(lm);
      //Debug.Log($"[Goodbye] openness={openness:F3}, timer={timer:F2}");

      float delta = 0f;
    if (lastTimestamp != -1)
    {
      delta = (timestampMillisec - lastTimestamp) / 1000f;
    }
    lastTimestamp = timestampMillisec;
    timer += delta;

      switch (state)
      {
        case State.None:
          if (openness > 0.25f)   // clearly open (you said max ~0.35)
          {
            openFrames++;

            if (openFrames >= MIN_OPEN_FRAMES)
            {
              openReference = openness;
              state = State.OpenDetected;
            }
          }
          else
          {
            openFrames = 0;
          }
          break;

        case State.OpenDetected:
          // fingers bend noticeably (not fist)
          if (openness < openReference * 0.6f)
          {
            Reset();
            Debug.Log($"👋 Goodbye detected (open={openReference:F2}, close={openness:F2})");
            return true;
          }

          break;
      }

      return false;
    }

    void Reset()
    {
      state = State.None;
      openFrames = 0;
      openReference = -1f;
    }


    // ---------- Helpers ----------

    bool IsPalmFacingCamera(NormalizedLandmarks lm)
    {
      Vector3 wrist = ToV3(lm.landmarks[0]);
      Vector3 indexBase = ToV3(lm.landmarks[5]);
      Vector3 pinkyBase = ToV3(lm.landmarks[17]);

      Vector3 normal = Vector3.Cross(
          indexBase - wrist,
          pinkyBase - wrist
      ).normalized;

      return normal.z < -0.2f;
    }


    float HandOpenness(NormalizedLandmarks lm)
    {
      Vector3 wrist = ToV3(lm.landmarks[0]);

      float sum =
          Vector3.Distance(ToV3(lm.landmarks[8]), wrist) +
          Vector3.Distance(ToV3(lm.landmarks[12]), wrist) +
          Vector3.Distance(ToV3(lm.landmarks[16]), wrist) +
          Vector3.Distance(ToV3(lm.landmarks[20]), wrist);

      return sum / 4f;
    }

    Vector3 ToV3(Mediapipe.Tasks.Components.Containers.NormalizedLandmark lm)
    {
      return new Vector3(lm.x, lm.y, lm.z);
    }
  }
}
