using UnityEngine;

/// <summary>摄像机平滑跟随玩家。</summary>
public class EasyFollowPlayer : MonoBehaviour
{
    #region 游戏逻辑

    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0f, 18f, -14f);
    [SerializeField] float positionSmooth = 6f;
    [SerializeField] float lookHeight = 0.6f;

    void Start()
    {
        if (target == null)
            target = GameObject.Find("Player")?.transform;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, positionSmooth * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }

    #endregion
}
