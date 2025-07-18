using UnityEngine;

public class GameCamera : MonoBehaviour
{
    private enum E_State
    {
        NotInitialized = 0,
        Agent = 1,
        Cutscene = 2
    }

    private const float DEFAULT_CAMERA_NEAR = 0.1f;

    public static GameCamera Instance;

    public CameraBehaviour CameraBehaviour;

    public Camera CameraWorld;

    public Camera CameraFPV;

    private Animation Animation;

    private Transform Transform;

    private float DisabledTime;

    private float DefaultFOV;

    private float ChangeFOVSpeed;

    private float DesiredFov;

    private float DesiredNear;

    private E_State State;

    public Vector3 CameraForward
    {
        get
        {
            return Transform.forward;
        }
    }

    private void Awake()
    {
        Instance = this;
        Animation = base.GetComponent<Animation>();
        Transform = base.transform;
        DesiredFov = DefaultFOV = CameraWorld.fieldOfView = 55f;
        DesiredNear = 0.1f;
        CameraWorld.nearClipPlane = 0.1f;
        CameraFPV.gameObject.SetActive(false);
        State = E_State.NotInitialized;
    }

    private void Start()
    {
        DisabledTime = 0f;
    }

    private void LateUpdate()
    {
        if (!(DisabledTime >= Time.timeSinceLevelLoad) && Time.deltaTime != 0f)
        {
            if (State == E_State.Agent)
            {
                Transform cameraWorldTransform = CameraBehaviour.GetCameraWorldTransform();
                Transform.position = cameraWorldTransform.position;
                Transform.rotation = cameraWorldTransform.rotation;
            }
            FPVShaderUtils.SetFPVProjectionParams(CameraWorld.fieldOfView, CameraFPV.fieldOfView, (float)Screen.height / (float)Screen.width, 0f);
        }
    }

    private void Update()
    {
        if (CameraWorld.fieldOfView < DesiredFov)
        {
            CameraWorld.fieldOfView = Mathf.Min(DesiredFov, CameraWorld.fieldOfView + ChangeFOVSpeed * TimeManager.Instance.GetRealDeltaTime());
        }
        else if (CameraWorld.fieldOfView > DesiredFov)
        {
            CameraWorld.fieldOfView = Mathf.Max(DesiredFov, CameraWorld.fieldOfView - ChangeFOVSpeed * TimeManager.Instance.GetRealDeltaTime());
        }
        if (CameraWorld.nearClipPlane < DesiredNear)
        {
            CameraWorld.nearClipPlane = Mathf.Min(DesiredNear, CameraWorld.nearClipPlane + 0.05f * ChangeFOVSpeed * TimeManager.Instance.GetRealDeltaTime());
        }
        else if (CameraWorld.nearClipPlane > DesiredNear)
        {
            CameraWorld.nearClipPlane = Mathf.Max(DesiredNear, CameraWorld.nearClipPlane - 0.05f * ChangeFOVSpeed * TimeManager.Instance.GetRealDeltaTime());
        }
    }

    public void PlayCameraAnim(string animName)
    {
        if (!(Animation[animName] == null))
        {
            Animation[animName].blendMode = AnimationBlendMode.Blend;
            Animation.CrossFade(animName, 0.5f);
        }
    }

    public void Reset()
    {
        CameraWorld.fieldOfView = DefaultFOV;
        DesiredFov = DefaultFOV;
    }

    public void Activate(Vector3 pos, Vector3 lookAt)
    {
        DisabledTime = 0f;
        Transform.position = pos;
        Transform.LookAt(lookAt);
    }

    public void SetFov(float fov, float speed, float near = 0.1f)
    {
        DesiredFov = fov;
        DesiredNear = near;
        ChangeFOVSpeed = speed;
    }

    public void SetDefaultFov(float speed)
    {
        SetFov(DefaultFOV, speed);
    }

    public void SetAgent(AgentHuman agent)
    {
        if ((bool)agent)
        {
            CameraBehaviour = agent.GetComponent<CameraBehaviourHuman>();
            State = E_State.Agent;
        }
        else
        {
            CameraBehaviour = null;
            State = E_State.NotInitialized;
        }
    }
}
