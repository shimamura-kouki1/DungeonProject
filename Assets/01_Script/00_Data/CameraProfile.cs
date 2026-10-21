using UnityEngine;

/// <summary>
/// 視点の種類。プレイヤー側が「向きの扱い」を決めるための区分で、
/// カメラ自身はこの値で挙動を変えない。
/// </summary>
public enum CameraViewMode
{
    ThirdPerson,
    FirstPerson,
}

/// <summary>
/// カメラの見え方を決める設定値。TPS用・FPS用などをアセットとして作り、
/// CameraRigに登録して切り替える。新しい視点が欲しくなったらアセットを1つ増やすだけ。
///
/// 作成: Project右クリック > Create > Camera > Camera Profile
/// </summary>
[CreateAssetMenu(menuName = "Camera/Camera Profile", fileName = "CameraProfile")]
public class CameraProfile : ScriptableObject
{
    [Header("視点の種類")]
    [Tooltip("モード変更の通知に使う。プレイヤー側がこれを見て向きの方式を決める")]
    public CameraViewMode ViewMode = CameraViewMode.ThirdPerson;

    [Header("注視点")]
    [Tooltip("target.position からの注視点のずれ(頭〜胸あたり)")]
    public Vector3 PivotOffset = new Vector3(0f, 1.5f, 0f);

    [Header("配置")]
    [Tooltip("注視点からの距離。0にすると一人称(自キャラは自動で非表示)")]
    public float Distance = 4f;
    [Tooltip("肩越し視点用。x=右にずらす量。一人称なら0")]
    public Vector3 ShoulderOffset = new Vector3(0.4f, 0f, 0f);
    public float Fov = 60f;

    [Header("上下の角度(度)  マイナス=見上げ / プラス=見下ろし")]
    public float MinPitch = -30f;
    public float MaxPitch = 70f;

    [Header("壁衝突")]
    public float CollisionRadius = 0.25f;
}
