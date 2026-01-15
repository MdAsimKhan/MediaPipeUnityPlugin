//using Mediapipe.Tasks.Vision.HandLandmarker;
//using Mediapipe.Tasks.Components.Containers;
//using UnityEngine;

//namespace Mediapipe.Unity.Sample.HandLandmarkDetection
//{
//  public class ASLYesNoDetector
//  {
//    public enum DetectedGesture
//    {
//      None,
//      Yes,
//      No
//    }

//    enum YesState { Idle, FistDetected, DipDetected }
//    enum NoState { Idle, OpenBeakDetected }

//    YesState yesState = YesState.Idle;
//    NoState noState = NoState.Idle;

//    long lastTimestamp = -1;
//    float yesTimer = 0f;
//    float noTimer = 0f;
//    float referencePitch = 0f;

//    // --- Config ---
//    const float GESTURE_TIMEOUT = 1.0f;

//    // YES (Fist Nod)
//    const float FIST_THRESHOLD = 0.22f;
//    const float NOD_PITCH_THRESHOLD = 0.035f;

//    // NO (Beak Tap)
//    const float BEAK_OPEN_DIST = 0.06f;
//    const float BEAK_CLOSED_DIST = 0.07f;
//    const float RING_PINKY_CLOSED_THRESHOLD = 0.20f;

//    // NEW: Minimum extension for Index/Middle to differentiate from Fist/Wave
//    const float INDEX_MIDDLE_EXTENDED_THRESHOLD = 0.25f;

//    public DetectedGesture Detect(HandLandmarkerResult result, long timestampMillisec)
//    {
//      if (result.handLandmarks == null || result.handLandmarks.Count == 0)
//      {
//        ResetAll();
//        return DetectedGesture.None;
//      }

//      var lm = result.handLandmarks[0];

//      // 1. Handedness
//      string mpLabel = "Left";
//      if (result.handedness != null && result.handedness.Count > 0 && result.handedness[0].categories.Count > 0)
//      {
//        mpLabel = result.handedness[0].categories[0].categoryName;
//      }
//      string userLabel = (mpLabel == "Right") ? "Left" : "Right";

//      // 2. Time Delta
//      float delta = 0f;
//      if (lastTimestamp != -1) delta = (timestampMillisec - lastTimestamp) / 1000f;
//      lastTimestamp = timestampMillisec;

//      // 3. Analyze Gestures

//      // PRIORITY CHECK: "No" requires a specific hand shape (2 fingers up, 2 down).
//      // "Yes" requires all fingers down.
//      // These shapes are mutually exclusive if we check extensions correctly.

//      if (CheckForYes(lm, delta))
//      {
//        Debug.Log($"✅ YES Detected on {userLabel} Hand");
//        ResetAll();
//        return DetectedGesture.Yes;
//      }

//      if (CheckForNo(lm, delta))
//      {
//        Debug.Log($"🚫 NO Detected on {userLabel} Hand");
//        ResetAll();
//        return DetectedGesture.No;
//      }

//      return DetectedGesture.None;
//    }

//    void ResetAll()
//    {
//      yesState = YesState.Idle;
//      noState = NoState.Idle;
//      yesTimer = 0f;
//      noTimer = 0f;
//    }

//    // -----------------------
//    // LOGIC: ASL "YES" (Fist Nod)
//    // -----------------------
//    bool CheckForYes(NormalizedLandmarks lm, float delta)
//    {
//      if (!IsFist(lm))
//      {
//        if (yesState == YesState.FistDetected) yesState = YesState.Idle;
//        if (GetAverageFingerDist(lm) > FIST_THRESHOLD * 1.5f) yesState = YesState.Idle;
//      }

//      float currentPitch = GetFistPitch(lm);

//      switch (yesState)
//      {
//        case YesState.Idle:
//          if (IsFist(lm))
//          {
//            referencePitch = currentPitch;
//            yesState = YesState.FistDetected;
//            yesTimer = 0f;
//          }
//          break;

//        case YesState.FistDetected:
//          yesTimer += delta;
//          if (currentPitch < referencePitch - NOD_PITCH_THRESHOLD)
//            yesState = YesState.DipDetected;
//          if (yesTimer > GESTURE_TIMEOUT) yesState = YesState.Idle;
//          break;

//        case YesState.DipDetected:
//          yesTimer += delta;
//          if (currentPitch > referencePitch - (NOD_PITCH_THRESHOLD * 0.5f))
//            return true;
//          if (yesTimer > GESTURE_TIMEOUT) yesState = YesState.Idle;
//          break;
//      }
//      return false;
//    }

//    // -----------------------
//    // LOGIC: ASL "NO" (Beak Tap)
//    // -----------------------
//    bool CheckForNo(NormalizedLandmarks lm, float delta)
//    {
//      // CRITICAL FIX: The shape MUST be "Two Fingers UP, Two Fingers DOWN".

//      // 1. Ring & Pinky must be tucked
//      if (!IsRingPinkyClosed(lm))
//      {
//        noState = NoState.Idle;
//        return false;
//      }

//      // 2. Index & Middle must be EXTENDED (This filters out Fists and Waving)
//      if (!IsIndexMiddleExtended(lm))
//      {
//        noState = NoState.Idle;
//        return false;
//      }

//      float beakDist = GetBeakDistance(lm);

//      switch (noState)
//      {
//        case NoState.Idle:
//          if (beakDist > BEAK_OPEN_DIST)
//          {
//            noState = NoState.OpenBeakDetected;
//            noTimer = 0f;
//          }
//          break;

//        case NoState.OpenBeakDetected:
//          noTimer += delta;
//          if (beakDist < BEAK_CLOSED_DIST)
//          {
//            return true;
//          }
//          if (noTimer > GESTURE_TIMEOUT) noState = NoState.Idle;
//          break;
//      }
//      return false;
//    }

//    // -----------------------
//    // HELPERS
//    // -----------------------

//    bool IsFist(NormalizedLandmarks lm)
//    {
//      return GetAverageFingerDist(lm) < FIST_THRESHOLD;
//    }

//    // NEW HELPER: Ensures Index and Middle are NOT curled
//    bool IsIndexMiddleExtended(NormalizedLandmarks lm)
//    {
//      Vector3 wrist = ToV3(lm.landmarks[0]);
//      float indexDist = Vector3.Distance(ToV3(lm.landmarks[8]), wrist);
//      float middleDist = Vector3.Distance(ToV3(lm.landmarks[12]), wrist);

//      // Average extension of Index/Middle must be > threshold
//      return (indexDist + middleDist) / 2f > INDEX_MIDDLE_EXTENDED_THRESHOLD;
//    }

//    float GetAverageFingerDist(NormalizedLandmarks lm)
//    {
//      Vector3 wrist = ToV3(lm.landmarks[0]);
//      float sum =
//          Vector3.Distance(ToV3(lm.landmarks[8]), wrist) +
//          Vector3.Distance(ToV3(lm.landmarks[12]), wrist) +
//          Vector3.Distance(ToV3(lm.landmarks[16]), wrist) +
//          Vector3.Distance(ToV3(lm.landmarks[20]), wrist);
//      return sum / 4f;
//    }

//    bool IsRingPinkyClosed(NormalizedLandmarks lm)
//    {
//      Vector3 wrist = ToV3(lm.landmarks[0]);
//      float ringDist = Vector3.Distance(ToV3(lm.landmarks[16]), wrist);
//      float pinkyDist = Vector3.Distance(ToV3(lm.landmarks[20]), wrist);
//      return (ringDist + pinkyDist) / 2f < RING_PINKY_CLOSED_THRESHOLD;
//    }

//    float GetFistPitch(NormalizedLandmarks lm)
//    {
//      Vector3 wrist = ToV3(lm.landmarks[0]);
//      Vector3 middleKnuckle = ToV3(lm.landmarks[9]);
//      return (wrist.y - middleKnuckle.y);
//    }

//    float GetBeakDistance(NormalizedLandmarks lm)
//    {
//      Vector3 thumbTip = ToV3(lm.landmarks[4]);
//      Vector3 indexTip = ToV3(lm.landmarks[8]);
//      Vector3 middleTip = ToV3(lm.landmarks[12]);
//      float d1 = Vector3.Distance(thumbTip, indexTip);
//      float d2 = Vector3.Distance(thumbTip, middleTip);
//      return (d1 + d2) / 2f;
//    }

//    Vector3 ToV3(Mediapipe.Tasks.Components.Containers.NormalizedLandmark lm)
//    {
//      return new Vector3(lm.x, lm.y, lm.z);
//    }
//  }
//}
//using Mediapipe.Tasks.Vision.HandLandmarker;
//using Mediapipe.Tasks.Components.Containers;
//using UnityEngine;

//namespace Mediapipe.Unity.Sample.HandLandmarkDetection
//{
//  public class ASLYesNoDetector
//  {
//    public enum DetectedGesture { None, Yes, No }

//    // --- States ---
//    enum YesState { Idle, FistDetected, DipDetected }
//    enum NoState { Idle, OpenBeakDetected }

//    YesState yesState = YesState.Idle;
//    NoState noState = NoState.Idle;

//    long lastTimestamp = -1;
//    float yesTimer = 0f;
//    float noTimer = 0f;
//    float referencePitch = 0f;

//    // --- Configuration ---

//    // TOGGLE THIS IN INSPECTOR IF NEEDED (via a wrapper) OR CODE
//    public bool debugMode = false;

//    // YES (Fist Nod)
//    const float GESTURE_TIMEOUT = 1.0f;
//    const float FIST_THRESHOLD = 0.30f; // Very relaxed fist (0.3 is quite open)
//    const float NOD_DROP_THRESHOLD = 0.03f; // How much Y must drop to count as a nod

//    // NO (Beak Tap)
//    const float BEAK_OPEN_DIST = 0.06f;
//    const float BEAK_CLOSED_DIST = 0.08f;
//    const float RING_PINKY_CLOSED_THRESHOLD = 0.18f;
//    const float INDEX_MIDDLE_EXTENDED_THRESHOLD = 0.20f;

//    public DetectedGesture Detect(HandLandmarkerResult result, long timestampMillisec)
//    {
//      if (result.handLandmarks == null || result.handLandmarks.Count == 0)
//      {
//        ResetAll();
//        return DetectedGesture.None;
//      }

//      var lm = result.handLandmarks[0];

//      // 1. Handedness
//      string mpLabel = (result.handedness != null && result.handedness.Count > 0) ? result.handedness[0].categories[0].categoryName : "Left";
//      string userLabel = (mpLabel == "Right") ? "Left" : "Right";

//      // 2. Time Delta
//      float delta = 0f;
//      if (lastTimestamp != -1) delta = (timestampMillisec - lastTimestamp) / 1000f;
//      lastTimestamp = timestampMillisec;

//      // 3. Logic

//      // We check NO first because it has specific finger requirements (Index/Middle extended)
//      if (CheckForNo(lm, delta))
//      {
//        Debug.Log($"🚫 NO Detected on {userLabel} Hand");
//        ResetAll();
//        return DetectedGesture.No;
//      }

//      if (CheckForYes(lm, delta, mpLabel))
//      {
//        Debug.Log($"✅ YES Detected on {userLabel} Hand");
//        ResetAll();
//        return DetectedGesture.Yes;
//      }

//      return DetectedGesture.None;
//    }

//    void ResetAll()
//    {
//      yesState = YesState.Idle;
//      noState = NoState.Idle;
//      yesTimer = 0f;
//      noTimer = 0f;
//    }

//    // -----------------------
//    // LOGIC: YES (Fist Nod)
//    // -----------------------
//    bool CheckForYes(NormalizedLandmarks lm, float delta, string mpLabel)
//    {
//      // Calculate metrics
//      bool isFist = IsLooseFist(lm);
//      float currentPitch = GetFistPitch(lm); // High = Upright, Low = Tilted Down

//      // Debugging
//      if (debugMode && yesState != YesState.Idle)
//      {
//        Debug.Log($"[YES DEBUG] State: {yesState}, Fist: {isFist}, Pitch: {currentPitch:F3}, Ref: {referencePitch:F3}");
//      }

//      switch (yesState)
//      {
//        case YesState.Idle:
//          // To START, we need a Fist AND Palm facing camera
//          if (isFist && IsPalmFacingCamera(lm, mpLabel))
//          {
//            referencePitch = currentPitch;
//            yesState = YesState.FistDetected;
//            yesTimer = 0f;
//          }
//          break;

//        case YesState.FistDetected:
//          yesTimer += delta;

//          // CRITICAL FIX: We DO NOT check IsPalmFacingCamera here.
//          // Nodding rotates the palm down, which would fail the check.

//          // If user opens hand, abort
//          if (!isFist)
//          {
//            // Allow small transient glitches, but if it's wide open, reset
//            if (GetAverageFingerDist(lm) > FIST_THRESHOLD * 1.2f) yesState = YesState.Idle;
//          }

//          // Check for Nod DOWN (Pitch decreases)
//          if (currentPitch < referencePitch - NOD_DROP_THRESHOLD)
//          {
//            yesState = YesState.DipDetected;
//          }

//          if (yesTimer > GESTURE_TIMEOUT) yesState = YesState.Idle;
//          break;

//        case YesState.DipDetected:
//          yesTimer += delta;

//          // Check for Nod UP (Return to start)
//          // We accept 50% return
//          if (currentPitch > referencePitch - (NOD_DROP_THRESHOLD * 0.5f))
//          {
//            return true;
//          }

//          if (yesTimer > GESTURE_TIMEOUT) yesState = YesState.Idle;
//          break;
//      }

//      return false;
//    }

//    // -----------------------
//    // LOGIC: NO (Beak Tap)
//    // -----------------------
//    bool CheckForNo(NormalizedLandmarks lm, float delta)
//    {
//      // 1. Ring/Pinky must be curled
//      if (!IsRingPinkyClosed(lm))
//      {
//        noState = NoState.Idle;
//        return false;
//      }
//      // 2. Index/Middle must be extended
//      if (!IsIndexMiddleExtended(lm))
//      {
//        noState = NoState.Idle;
//        return false;
//      }

//      float beakDist = GetBeakDistance(lm);

//      if (debugMode && noState != NoState.Idle)
//        Debug.Log($"[NO DEBUG] Beak: {beakDist:F3}");

//      switch (noState)
//      {
//        case NoState.Idle:
//          if (beakDist > BEAK_OPEN_DIST)
//          {
//            noState = NoState.OpenBeakDetected;
//            noTimer = 0f;
//          }
//          break;

//        case NoState.OpenBeakDetected:
//          noTimer += delta;
//          if (beakDist < BEAK_CLOSED_DIST)
//          {
//            return true;
//          }
//          if (noTimer > GESTURE_TIMEOUT) noState = NoState.Idle;
//          break;
//      }
//      return false;
//    }

//    // -----------------------
//    // HELPERS
//    // -----------------------

//    // Loose fist: Average finger tip distance from wrist is small
//    bool IsLooseFist(NormalizedLandmarks lm)
//    {
//      return GetAverageFingerDist(lm) < FIST_THRESHOLD;
//    }

//    // Checks if Index and Middle are sticking out (preventing Fist/Yes conflict)
//    bool IsIndexMiddleExtended(NormalizedLandmarks lm)
//    {
//      Vector3 wrist = ToV3(lm.landmarks[0]);
//      float indexDist = Vector3.Distance(ToV3(lm.landmarks[8]), wrist);
//      float middleDist = Vector3.Distance(ToV3(lm.landmarks[12]), wrist);
//      return (indexDist + middleDist) / 2f > INDEX_MIDDLE_EXTENDED_THRESHOLD;
//    }

//    bool IsRingPinkyClosed(NormalizedLandmarks lm)
//    {
//      Vector3 wrist = ToV3(lm.landmarks[0]);
//      float ringDist = Vector3.Distance(ToV3(lm.landmarks[16]), wrist);
//      float pinkyDist = Vector3.Distance(ToV3(lm.landmarks[20]), wrist);
//      return (ringDist + pinkyDist) / 2f < RING_PINKY_CLOSED_THRESHOLD;
//    }

//    float GetAverageFingerDist(NormalizedLandmarks lm)
//    {
//      Vector3 wrist = ToV3(lm.landmarks[0]);
//      float sum =
//          Vector3.Distance(ToV3(lm.landmarks[8]), wrist) +
//          Vector3.Distance(ToV3(lm.landmarks[12]), wrist) +
//          Vector3.Distance(ToV3(lm.landmarks[16]), wrist) +
//          Vector3.Distance(ToV3(lm.landmarks[20]), wrist);
//      return sum / 4f;
//    }

//    // Measures vertical difference between Wrist and Middle Knuckle
//    float GetFistPitch(NormalizedLandmarks lm)
//    {
//      // MP coords: Y=0 is Top, Y=1 is Bottom.
//      // Upright hand: Wrist(0.8) - Knuckle(0.6) = 0.2 (Positive)
//      // Nod down: Wrist(0.8) - Knuckle(0.7) = 0.1 (Decreases)
//      return ToV3(lm.landmarks[0]).y - ToV3(lm.landmarks[9]).y;
//    }

//    float GetBeakDistance(NormalizedLandmarks lm)
//    {
//      Vector3 thumbTip = ToV3(lm.landmarks[4]);
//      float d1 = Vector3.Distance(thumbTip, ToV3(lm.landmarks[8]));
//      float d2 = Vector3.Distance(thumbTip, ToV3(lm.landmarks[12]));
//      return (d1 + d2) / 2f;
//    }

//    bool IsPalmFacingCamera(NormalizedLandmarks lm, string rawMpLabel)
//    {
//      Vector3 wrist = ToV3(lm.landmarks[0]);
//      Vector3 indexBase = ToV3(lm.landmarks[5]);
//      Vector3 pinkyBase = ToV3(lm.landmarks[17]);
//      Vector3 normal = Vector3.Cross(indexBase - wrist, pinkyBase - wrist).normalized;

//      // Check based on handedness
//      if (rawMpLabel == "Right") return normal.z > 0.1f;
//      else return normal.z < -0.1f;
//    }

//    Vector3 ToV3(Mediapipe.Tasks.Components.Containers.NormalizedLandmark lm)
//    {
//      return new Vector3(lm.x, lm.y, lm.z);
//    }
//  }
//}

using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Components.Containers;
using UnityEngine;

namespace Mediapipe.Unity.Sample.HandLandmarkDetection
{
  public class ASLYesNoDetector
  {
    public enum DetectedGesture
    {
      None,
      Yes,
      No
    }

    // --- States ---
    enum YesState { Idle, FistDetected, DipDetected }
    enum NoState { Idle, OpenBeakDetected }

    YesState yesState = YesState.Idle;
    NoState noState = NoState.Idle;

    long lastTimestamp = -1;
    float yesTimer = 0f;
    float noTimer = 0f;
    float referencePitch = 0f;

    // --- Config ---
    const float GESTURE_TIMEOUT = 1.0f;

    // YES Config (From your working script)
    const float FIST_THRESHOLD = 0.22f;
    const float NOD_PITCH_THRESHOLD = 0.035f;

    // NO Config (From the latest robust script)
    const float BEAK_OPEN_DIST = 0.06f;
    const float BEAK_CLOSED_DIST = 0.08f; // Relaxed for easier tap
    const float RING_PINKY_CLOSED_THRESHOLD = 0.18f;
    const float INDEX_MIDDLE_EXTENDED_THRESHOLD = 0.20f;

    public DetectedGesture Detect(HandLandmarkerResult result, long timestampMillisec)
    {
      if (result.handLandmarks == null || result.handLandmarks.Count == 0)
      {
        ResetAll();
        return DetectedGesture.None;
      }

      var lm = result.handLandmarks[0];

      // 1. Handedness
      string mpLabel = (result.handedness != null && result.handedness.Count > 0) ? result.handedness[0].categories[0].categoryName : "Left";
      string userLabel = (mpLabel == "Right") ? "Left" : "Right";

      // 2. Time Delta
      float delta = 0f;
      if (lastTimestamp != -1) delta = (timestampMillisec - lastTimestamp) / 1000f;
      lastTimestamp = timestampMillisec;

      // 3. Logic

      // Check NO first (Specific finger shape)
      if (CheckForNo(lm, delta))
      {
        Debug.Log($"🚫 NO Detected on {userLabel} Hand");
        ResetAll();
        return DetectedGesture.No;
      }

      // Check YES second (General fist shape)
      if (CheckForYes(lm, delta))
      {
        Debug.Log($"✅ YES Detected on {userLabel} Hand");
        ResetAll();
        return DetectedGesture.Yes;
      }

      return DetectedGesture.None;
    }

    void ResetAll()
    {
      yesState = YesState.Idle;
      noState = NoState.Idle;
      yesTimer = 0f;
      noTimer = 0f;
    }

    // -----------------------
    // LOGIC: YES (From Your Script)
    // -----------------------
    bool CheckForYes(NormalizedLandmarks lm, float delta)
    {
      if (!IsFist(lm))
      {
        if (yesState == YesState.FistDetected) yesState = YesState.Idle;
        if (GetAverageFingerDist(lm) > FIST_THRESHOLD * 1.5f) yesState = YesState.Idle;
      }

      float currentPitch = GetFistPitch(lm);

      switch (yesState)
      {
        case YesState.Idle:
          if (IsFist(lm))
          {
            referencePitch = currentPitch;
            yesState = YesState.FistDetected;
            yesTimer = 0f;
          }
          break;

        case YesState.FistDetected:
          yesTimer += delta;
          if (currentPitch < referencePitch - NOD_PITCH_THRESHOLD)
            yesState = YesState.DipDetected;
          if (yesTimer > GESTURE_TIMEOUT) yesState = YesState.Idle;
          break;

        case YesState.DipDetected:
          yesTimer += delta;
          if (currentPitch > referencePitch - (NOD_PITCH_THRESHOLD * 0.5f))
            return true;
          if (yesTimer > GESTURE_TIMEOUT) yesState = YesState.Idle;
          break;
      }
      return false;
    }

    // -----------------------
    // LOGIC: NO (From Robust Script)
    // -----------------------
    bool CheckForNo(NormalizedLandmarks lm, float delta)
    {
      // 1. Ring/Pinky must be curled
      if (!IsRingPinkyClosed(lm))
      {
        noState = NoState.Idle;
        return false;
      }
      // 2. Index/Middle must be extended (Prevents conflict with Fist)
      if (!IsIndexMiddleExtended(lm))
      {
        noState = NoState.Idle;
        return false;
      }

      float beakDist = GetBeakDistance(lm);

      switch (noState)
      {
        case NoState.Idle:
          if (beakDist > BEAK_OPEN_DIST)
          {
            noState = NoState.OpenBeakDetected;
            noTimer = 0f;
          }
          break;

        case NoState.OpenBeakDetected:
          noTimer += delta;
          if (beakDist < BEAK_CLOSED_DIST)
          {
            return true;
          }
          if (noTimer > GESTURE_TIMEOUT) noState = NoState.Idle;
          break;
      }
      return false;
    }

    // -----------------------
    // HELPERS
    // -----------------------

    bool IsFist(NormalizedLandmarks lm)
    {
      return GetAverageFingerDist(lm) < FIST_THRESHOLD;
    }

    // Helper for No: Ensures Index and Middle are NOT curled
    bool IsIndexMiddleExtended(NormalizedLandmarks lm)
    {
      Vector3 wrist = ToV3(lm.landmarks[0]);
      float indexDist = Vector3.Distance(ToV3(lm.landmarks[8]), wrist);
      float middleDist = Vector3.Distance(ToV3(lm.landmarks[12]), wrist);

      // Average extension must be > threshold
      return (indexDist + middleDist) / 2f > INDEX_MIDDLE_EXTENDED_THRESHOLD;
    }

    bool IsRingPinkyClosed(NormalizedLandmarks lm)
    {
      Vector3 wrist = ToV3(lm.landmarks[0]);
      float ringDist = Vector3.Distance(ToV3(lm.landmarks[16]), wrist);
      float pinkyDist = Vector3.Distance(ToV3(lm.landmarks[20]), wrist);
      return (ringDist + pinkyDist) / 2f < RING_PINKY_CLOSED_THRESHOLD;
    }

    float GetAverageFingerDist(NormalizedLandmarks lm)
    {
      Vector3 wrist = ToV3(lm.landmarks[0]);
      float sum =
          Vector3.Distance(ToV3(lm.landmarks[8]), wrist) +
          Vector3.Distance(ToV3(lm.landmarks[12]), wrist) +
          Vector3.Distance(ToV3(lm.landmarks[16]), wrist) +
          Vector3.Distance(ToV3(lm.landmarks[20]), wrist);
      return sum / 4f;
    }

    float GetFistPitch(NormalizedLandmarks lm)
    {
      Vector3 wrist = ToV3(lm.landmarks[0]);
      Vector3 middleKnuckle = ToV3(lm.landmarks[9]);
      return (wrist.y - middleKnuckle.y);
    }

    float GetBeakDistance(NormalizedLandmarks lm)
    {
      Vector3 thumbTip = ToV3(lm.landmarks[4]);
      // Distance from thumb tip to average of Index(8) and Middle(12) tips
      float d1 = Vector3.Distance(thumbTip, ToV3(lm.landmarks[8]));
      float d2 = Vector3.Distance(thumbTip, ToV3(lm.landmarks[12]));
      return (d1 + d2) / 2f;
    }

    Vector3 ToV3(Mediapipe.Tasks.Components.Containers.NormalizedLandmark lm)
    {
      return new Vector3(lm.x, lm.y, lm.z);
    }
  }
}
