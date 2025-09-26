using UnityEngine;

namespace WhaleShark.Config
{
    [CreateAssetMenu(menuName = "WhaleShark/Config/MoveConfig")]
    public class MoveConfig : ScriptableObject
    {
        [Header("Movement")]
        public float moveSpeed = 6f;
        public float jumpPower = 7f;
        public float airControl = 0.8f;

        [Header("Ground Check")]
        public LayerMask groundMask = ~0;
        public float groundCheckDist = 0.1f;
        public float coyoteTime = 0.1f;

        [Header("Jump")]
        public float jumpBufferTime = 0.1f;
        public int maxJumps = 1;
        public float fallMultiplier = 2.5f;
        public float lowJumpMultiplier = 2f;
    }
}