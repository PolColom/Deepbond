using UnityEngine;

public class LedgeDetection : MonoBehaviour
{
    [SerializeField] public float radius;
    [SerializeField] public LayerMask ledgeMask;
    [SerializeField] private Player_Movement player;

    private void Update(){
        Collider2D ledgeCollider = Physics2D.OverlapCircle(transform.position, radius, ledgeMask);

        if (ledgeCollider != null){
            // Només agafa la "Ledge" si el jugador està sota la paret
            if (transform.position.y > ledgeCollider.bounds.center.y){
                player.GrabLedge(ledgeCollider.transform.position, false);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}