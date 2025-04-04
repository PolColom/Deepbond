using UnityEngine;

public class WallCheck : MonoBehaviour
{

    [Header("Configuració")]
    [SerializeField] private float wallCheckDistance;
    [SerializeField] private LayerMask surfaceMask;
    [SerializeField] private Player_Movement player;
    [SerializeField] private float wallSlideSpeed = 7.0f;


    [Header("Estat")]
    public bool isTouchingWall;
    public bool isTouchingWallLeft;
    public bool isTouchingWallRight;
    private RaycastHit2D wallHit;

    private void Update()
    {
        CheckWall();
        HandleWallSlide();
    }

    private void CheckWall()
    {
        // Comprovació de parets amb raycasts a esquerra i dreta
        isTouchingWallLeft = Physics2D.Raycast(transform.position, Vector2.left, wallCheckDistance, surfaceMask);
        isTouchingWallRight = Physics2D.Raycast(transform.position, Vector2.right, wallCheckDistance, surfaceMask);

        // Guarda si està tocant alguna paret en general
        isTouchingWall = isTouchingWallLeft || isTouchingWallRight;

        // Actualitza l'estat al Player_Movement
        player.isTouchingWall = isTouchingWall;

        // Dibuixa els raycasts a l'Scene View (útil per debuggar)
        Debug.DrawRay(transform.position, Vector2.left * wallCheckDistance, Color.red);
        Debug.DrawRay(transform.position, Vector2.right * wallCheckDistance, Color.red);
    }


    private void HandleWallSlide()
    {
        // Si està tocant una paret, no està a terra, està caient i no està agafat a una vora
        if (isTouchingWall && !player.isGrounded && player.body.linearVelocity.y < 0 && !player.isGrabbingLedge)
        {
            // Bloqueja el moviment horitzontal
            player.body.linearVelocity = new Vector2(0, Mathf.Max(player.body.linearVelocity.y, -wallSlideSpeed));

            // Manté una gravetat reduïda perquè caigui però sense quedar-se enganxat
            player.body.gravityScale = 1f;

            // Activar l'animació de Wall Slide
            player.SetWallSliding(true);
        }
        else
        {
            // Quan deixi de fer Wall Slide (només quan toqui terra), restaurar la gravetat normal
            if (player.isGrounded)
            {
                player.body.gravityScale = 1f;
            }

            player.SetWallSliding(false);
        }
    }


    // Direcció del salt contra la paret (útil pel wall jump)
    public float GetWallJumpDirection()
    {
        if (isTouchingWallLeft) return 1f;  // Salt cap a la dreta
        if (isTouchingWallRight) return -1f; // Salt cap a l'esquerra
        return 0f;  // No hi ha paret
    }

    // Per visualitzar els raycasts en l'Scene View
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * wallCheckDistance);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.left * wallCheckDistance);
    }
}