using UnityEngine;

namespace FYP
{
    // player colors per phase and the hit sphere
    public class PlayerCombatVisuals : MonoBehaviour
    {
        public Material hitSphereMaterial;

        [Header("Body colors")]
        public Color lightWindupColor = new Color(1f, 0.9f, 0.2f);
        public Color heavyWindupColor = new Color(1f, 0.5f, 0.1f);
        public Color activeColor = new Color(1f, 0.15f, 0.15f);
        public Color recoveryColor = new Color(0.45f, 0.45f, 0.5f);
        public Color dodgeColor = new Color(0.3f, 1f, 1f);

        FYPPlayerController player;
        DamageFeedback damageFeedback;
        Renderer body;
        Color normalColor;
        GameObject hitSphere;

        // remember color, create the hit sphere
        void Start()
        {
            player = GetComponent<FYPPlayerController>();
            damageFeedback = GetComponent<DamageFeedback>();
            body = GetComponent<Renderer>();
            normalColor = body.material.color;

            // own object, not a child: the player is squashed and would squash the sphere too
            hitSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hitSphere.name = "HitSphere";
            Destroy(hitSphere.GetComponent<Collider>());
            hitSphere.GetComponent<Renderer>().material = hitSphereMaterial;
            hitSphere.SetActive(false);
        }

        // update color and sphere every frame
        void Update()
        {
            // the white hit flash is more important than the phase color
            if (damageFeedback == null || !damageFeedback.isFlashing)
                body.material.color = GetStateColor();

            if (player.state == CombatState.Active)
            {
                hitSphere.SetActive(true);
                hitSphere.transform.position = player.hitCenter;
                hitSphere.transform.localScale = Vector3.one * player.hitRadius * 2;
            }
            else
            {
                hitSphere.SetActive(false);
            }
        }

        // color for the current phase
        Color GetStateColor()
        {
            if (player.state == CombatState.Windup && player.currentAction == PlayerAction.HeavyAttack)
                return heavyWindupColor;
            if (player.state == CombatState.Windup)
                return lightWindupColor;
            if (player.state == CombatState.Active)
                return activeColor;
            if (player.state == CombatState.Recovery)
                return recoveryColor;
            if (player.state == CombatState.Dodge)
                return dodgeColor;
            return normalColor;
        }
    }
}
