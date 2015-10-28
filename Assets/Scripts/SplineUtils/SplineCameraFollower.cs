using UnityEngine;
using System.Collections;

public class SplineCameraFollower : MonoBehaviour {

	public CameraShot[] Shots;
	public Transform Camera;
	public Transform Fade;
	public float FadeLength;

	private int CurrentShot = 0;
	private float SplineProgress = 0;
    private float FadeStartTime = -1;
    private FadeState CurrentFade = FadeState.None;
	private Material FadeMaterial;

	void Start () {
		if(Shots.Length == 0 || Camera == null)
			return;

		FadeMaterial = Fade.GetComponent<Renderer>().material;
		ApplyShot();
	}

	void Update () {
        ApplyShot();
        HandleShotState();

        if (CurrentFade != FadeState.None)
        {
            Color col = FadeMaterial.GetColor("_Color");
            float alpha = Mathf.Clamp01((Time.time - FadeStartTime) / FadeLength);
            
            if (CurrentFade == FadeState.FadeIn)
                alpha = 1 - alpha;

            col.a = alpha;
            FadeMaterial.SetColor("_Color", col);
        }
    }

    private void HandleShotState()
    {
        if (CurrentFade == FadeState.None)
        {
            SplineProgress += (Shots[CurrentShot].Velocity / Shots[CurrentShot].Path.GetDeriv(SplineProgress).magnitude)*Time.deltaTime;
            Vector3 diff = Shots[CurrentShot].Path.GetPoint(Shots[CurrentShot].Path.GetCurveCount())
                         - Shots[CurrentShot].Path.GetPoint(SplineProgress);

            if ((float)Shots[CurrentShot].Path.GetCurveCount() - SplineProgress < 0.05f)
            {
                CurrentFade = FadeState.FadeOut;
                FadeStartTime = Time.time;
            }
        }
        else if (Time.time >= FadeStartTime + FadeLength)
        {
            if (CurrentFade == FadeState.FadeIn)
            {
                FadeStartTime = -1;
                CurrentFade = FadeState.None;
            } else
            {
                CurrentShot++;
                CurrentShot %= Shots.Length;
                SplineProgress = 0;
                FadeStartTime = Time.time;

                CurrentFade = FadeState.FadeIn;
            }
        }
    }

	private void ApplyShot() {
		Camera.position = Shots[CurrentShot].Path.GetPoint(SplineProgress);
		if(Shots[CurrentShot].LookAt != null)
			Camera.LookAt(Shots[CurrentShot].LookAt);
		else
			Camera.rotation = Quaternion.LookRotation(Shots[CurrentShot].Path.GetDirection(SplineProgress));
	}

	[System.Serializable]
	public class CameraShot {
		public Transform LookAt;
		public Spline Path;
		public float Velocity;
	}

    private enum FadeState
    {
       FadeOut, FadeIn, None
    }
}
