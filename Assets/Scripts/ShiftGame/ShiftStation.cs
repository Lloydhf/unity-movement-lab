using UnityEngine;

namespace ShiftGame
{
    public sealed class ShiftStation : MonoBehaviour
    {
        [Tooltip("Oda günlüğünde bu istasyonu tanıtan ad.")]
        public string stationId;
        [Tooltip("Oda yöneticisinin kullanabileceği güvenli doğma konumu.")]
        public Transform spawnPoint;

        public bool CanUse(ShiftRobot robot)
        {
            if (robot == null || !robot.IsGrounded) return false;
            Vector3 offset = robot.transform.position - transform.position;
            return Mathf.Abs(offset.x) <= 1.5f && Mathf.Abs(offset.y) <= 1.1f;
        }

        public bool TryUse(ShiftRobot robot)
        {
            if (!CanUse(robot)) return false;
            robot.SetHeavy(!robot.IsHeavy);
            return true;
        }
    }
}
