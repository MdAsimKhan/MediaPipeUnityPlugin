using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Components.Containers;
using UnityEngine;

namespace Mediapipe.Unity.Sample.HandLandmarkDetection
{
  public class ASLGoodbyeDetector
  {
    enum State
    {
      Idle,
      OpenDetected,   // Phase 1: Hand is clearly open
      ClosingDetected // Phase 2: Hand has closed (mid-wave)
    }

    State state = State.Idle;

    int openFrames = 0;
    float timer = 0f;
    float highOpennessRef = -1f; // The "High" point of the wave
    long lastTimestamp = -1;

    // --- Config ---
    const float WAVE_TIMEOUT = 1.5f; // Max time to complete Open->Close->Open
    const int MIN_OPEN_FRAMES = 5;
    const bool IS_CAMERA_MIRRORED = true; // Set true for webcams

    public bool Detect(HandLandmarkerResult result, long timestampMillisec)
    {
      if (result.handLandmarks == null || result.handLandmarks.Count == 0)
      {
        Reset();
        return false;
      }

      var lm = result.handLandmarks[0];

      // 1. Handedness Logic (Match MP view to Reality)
      string mpLabel = "Left";
      if (result.handedness != null && result.handedness.Count > 0 && result.handedness[0].categories.Count > 0)
      {
        mpLabel = result.handedness[0].categories[0].categoryName;
      }
      string userLabel = (mpLabel == "Right") ? "Left" : "Right";

      // 2. Palm Facing Check
      if (!IsPalmFacingCamera(lm, mpLabel))
      {
        Reset();
        return false;
      }

      // 3. Openness Calculation
      float openness = HandOpenness(lm);

      // 4. Time Delta
      float delta = 0f;
      if (lastTimestamp != -1) delta = (timestampMillisec - lastTimestamp) / 1000f;
      lastTimestamp = timestampMillisec;

      // 5. CYCLIC STATE MACHINE
      switch (state)
      {
        case State.Idle:
          // Step 1: Detect Open Hand
          if (openness > 0.25f)
          {
            openFrames++;
            if (openFrames >= MIN_OPEN_FRAMES)
            {
              highOpennessRef = openness;
              state = State.OpenDetected;
              timer = 0f;
            }
          }
          else
          {
            openFrames = 0;
          }
          break;

        case State.OpenDetected:
          timer += delta;

          // Step 2: Detect Closing (The dip in the wave)
          // Must close significantly ( < 60% of original open size)
          if (openness < highOpennessRef * 0.6f)
          {
            state = State.ClosingDetected;
            // Keep timer running; don't reset it
          }

          // Timeout if hand stays open too long without waving
          if (timer > WAVE_TIMEOUT) Reset();
          break;

        case State.ClosingDetected:
          timer += delta;

          // Step 3: Detect Re-Opening (The finish of the wave)
          // We don't need it to be 100% open, just ~85% of the start
          if (openness > highOpennessRef * 0.85f)
          {
            Debug.Log($"👋 Goodbye (Wave) Detected on {userLabel} Hand");
            Reset(); // Reset to detect next wave
            return true;
          }

          // TIMEOUT / FAIL SAFE:
          // If you hold the closed state too long (e.g., > 0.8s), 
          // you are likely making a FIST for "Yes", not waving.
          // This is the critical fix for the conflict.
          if (timer > WAVE_TIMEOUT || (timer > 0.8f && openness < 0.15f))
          {
            Reset();
          }
          break;
      }

      return false;
    }

    void Reset()
    {
      state = State.Idle;
      openFrames = 0;
      highOpennessRef = -1f;
      timer = 0f;
    }

    // ---------- Helpers ----------

    bool IsPalmFacingCamera(NormalizedLandmarks lm, string rawMpLabel)
    {
      Vector3 wrist = ToV3(lm.landmarks[0]);
      Vector3 indexBase = ToV3(lm.landmarks[5]);
      Vector3 pinkyBase = ToV3(lm.landmarks[17]);

      Vector3 normal = Vector3.Cross(indexBase - wrist, pinkyBase - wrist).normalized;

      if (rawMpLabel == "Left") return normal.z > 0.2f;
      else return normal.z < -0.2f;
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
