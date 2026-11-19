using UnityEngine;

namespace FYP
{
    // player colors per phase and the hit sphere
    public class PlayerCombatVisuals : MonoBehaviour
    {
        public Material hitSphereMaterial;
        public bool showHitSphere = false;

        [Header("Body colors")]
        public Color lightWindupColor = new Color(1f, 0.9f, 0.2f);
        public Color heavyWindupColor = new Color(1f, 0.5f, 0.1f);
        public Color activeColor = new Color(1f, 0.15f, 0.15f);
        public Color recoveryColor = new Color(0.45f, 0.45f, 0.5f);
        public Color dodgeColor = new Color(0.3f, 1f, 1f);

        [Header("Magic")]
        public Material magicMaterial;
        public Color shockwaveColor = new Color(0f, 0f, 0f, 0.4f);

        FYPPlayerController player;
        DamageFeedback damageFeedback;
        Renderer body;
        Color normalColor;
        GameObject hitSphere;
        Transform leftHand;
        Transform rightHand;
        TrailRenderer swordTrail;

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

            player.onHitWindowOpened += OnHitWindowOpened;

            MagicFx.material = magicMaterial;
            foreach (Transform t in GetComponentsInChildren<Transform>())
            {
                if (t.name == "mixamorig:LeftHand")
                    leftHand = t;
                if (t.name == "mixamorig:RightHand")
                    rightHand = t;
            }
            player.onActionPerformed += OnAction;
            player.onSpellReleased += OnSpellReleased;
            player.onHitLanded += (point, towardPlayer, damage) => CombatFx.Blood(point, towardPlayer, damage);
            swordTrail = MakeSwordTrail();
        }

        TrailRenderer MakeSwordTrail()
        {
            Transform sword = null;
            foreach (Transform t in GetComponentsInChildren<Transform>())
            {
                if (t.name == "Sword" && t.GetComponent<MeshFilter>() != null)
                    sword = t;
            }
            if (sword == null)
                return null;
            Bounds b = sword.GetComponent<MeshFilter>().sharedMesh.bounds;
            Vector3 size = b.size;
            Vector3 along = size.x >= size.y && size.x >= size.z ? Vector3.right : size.y >= size.z ? Vector3.up : Vector3.forward;
            Vector3 across = size.x <= size.y && size.x <= size.z ? Vector3.right : size.y <= size.z ? Vector3.up : Vector3.forward;
            Vector3 hand = sword.InverseTransformPoint(sword.parent.position);
            float half = Vector3.Dot(size, along) / 2;
            Vector3 endA = b.center + along * half;
            Vector3 endB = b.center - along * half;
            Vector3 tip = (endA - hand).sqrMagnitude > (endB - hand).sqrMagnitude ? endA : endB;
            Vector3 grip = tip == endA ? endB : endA;

            GameObject holder = new GameObject("SwordTrail");
            holder.transform.SetParent(sword, false);
            holder.transform.localPosition = Vector3.Lerp(grip, tip, 0.7f);
            holder.transform.localRotation = Quaternion.LookRotation(across, tip - grip);
            float bladeLength = Vector3.Distance(sword.TransformPoint(grip), sword.TransformPoint(tip));
            return CombatFx.Trail(holder.transform, bladeLength * 0.7f, 0.2f, true);
        }

        bool SwordSwinging()
        {
            AttackSettings attack = player.currentAttack;
            if (attack == null || attack == player.groundSlam || System.Array.IndexOf(player.kickCombo, attack) >= 0)
                return false;
            foreach (Vector2Int w in attack.hitFrames)
            {
                if (player.attackFrame >= w.x - 4 && player.attackFrame <= w.y + 1)
                    return true;
            }
            return false;
        }

        void OnAction(PlayerAction action)
        {
            AttackSettings attack = player.currentAttack;
            if (attack == null)
                return;
            if (action == PlayerAction.Spell && leftHand != null)
                MagicFx.Gather(leftHand, attack.TimeOf(attack.releaseFrame), 0.45f);
            if (action == PlayerAction.GroundSlam)
            {
                float until = attack.TimeOf(attack.hitFrames[0].x);
                if (leftHand != null)
                    MagicFx.Gather(leftHand, until, 0.7f);
                if (rightHand != null)
                    MagicFx.Gather(rightHand, until, 0.7f);
            }
        }

        void OnSpellReleased(GameObject ball)
        {
            MagicFx.Core(ball.transform);
            ParticleSystem trail = MagicFx.Trail(ball.transform);
            ball.GetComponent<Projectile>().onResolved += dealt =>
            {
                trail.transform.SetParent(null, true);
                trail.Stop();
                MagicFx.Burst(ball.transform.position, dealt > 0 ? 120 : 60, 6f, 0.4f);
                if (dealt > 0)
                    CombatFx.Blood(ball.transform.position, -ball.transform.forward, dealt);
            };
        }

        void OnHitWindowOpened(AttackSettings attack, int window)
        {
            if (attack != player.groundSlam)
                return;
            Vector3 floor = transform.position;
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 5f, ~0, QueryTriggerInteraction.Ignore))
                floor = hit.point;
            float size = (attack.hitRadius + attack.reach) * 2;
            MagicFx.GroundBurst(floor, size / 2);
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Shockwave";
            Destroy(ring.GetComponent<Collider>());
            Renderer r = ring.GetComponent<Renderer>();
            r.material = hitSphereMaterial;
            r.material.color = shockwaveColor;
            ring.transform.position = floor + Vector3.up * 0.05f;
            ring.AddComponent<Shockwave>().size = size;
        }

        // update color and sphere every frame
        void Update()
        {
            // the white hit flash is more important than the phase color
            if (damageFeedback == null || !damageFeedback.isFlashing)
                body.material.color = GetStateColor();
            if (swordTrail != null)
                swordTrail.emitting = SwordSwinging();

            if (showHitSphere && player.state == CombatState.Active)
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
