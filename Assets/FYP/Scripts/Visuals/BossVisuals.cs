using UnityEngine;

namespace FYP
{
    // boss colors per phase and the attack area drawn on the floor
    public class BossVisuals : MonoBehaviour
    {
        public Material telegraphMaterial;
        public bool showAttackArea = false;

        [Header("Body colors")]
        public Color windupColor = new Color(1f, 0.55f, 0.1f);
        public Color activeColor = new Color(1f, 0.1f, 0.1f);
        public Color recoveryColor = new Color(0.4f, 0.4f, 0.45f);

        [Header("Floor area colors")]
        public Color windupAreaColor = new Color(1f, 0.6f, 0.1f, 0.25f);
        public Color activeAreaColor = new Color(1f, 0.1f, 0.1f, 0.55f);

        [Header("Attack effects")]
        public Material fxMaterial;
        public float trailSpeed = 8f;
        public float dustHeight = 2f;

        BossController boss;
        DamageFeedback damageFeedback;
        Renderer body;
        Color normalColor;

        GameObject telegraph;
        MeshFilter telegraphMesh;
        Renderer telegraphRenderer;
        int meshBuiltForAttack = -1;

        Transform hammerHead;
        TrailRenderer hammerTrail;
        Vector3 lastHeadPosition;
        float attackStartTime;
        BossState lastState;

        // remember color, create the floor shape
        void Start()
        {
            boss = GetComponent<BossController>();
            damageFeedback = GetComponent<DamageFeedback>();
            body = GetComponent<Renderer>();
            normalColor = body.material.color;

            // own object, not a child: the boss is stretched and would stretch the floor shape too
            telegraph = new GameObject("BossTelegraph");
            telegraphMesh = telegraph.AddComponent<MeshFilter>();
            telegraphRenderer = telegraph.AddComponent<MeshRenderer>();
            telegraphRenderer.material = telegraphMaterial;
            telegraph.SetActive(false);

            if (MagicFx.material == null)
                MagicFx.material = fxMaterial;
            hammerHead = MakeHammerHead();
            if (hammerHead != null)
            {
                hammerTrail = CombatFx.Trail(hammerHead, 2.2f, 0.22f, false);
                lastHeadPosition = hammerHead.position;
            }
            boss.onAttackStarted += index => attackStartTime = Time.time;
        }

        Transform MakeHammerHead()
        {
            Transform hammer = null;
            Transform hand = null;
            foreach (Transform t in GetComponentsInChildren<Transform>())
            {
                if (t.name == "Knell_Hammer" && t.GetComponent<MeshFilter>() != null)
                    hammer = t;
                if (t.name == "RightHand")
                    hand = t;
            }
            if (hammer == null)
                return null;
            Bounds b = hammer.GetComponent<MeshFilter>().sharedMesh.bounds;
            Vector3 size = b.size;
            Vector3 along = size.x >= size.y && size.x >= size.z ? Vector3.right : size.y >= size.z ? Vector3.up : Vector3.forward;
            float half = Vector3.Dot(size, along) / 2;
            Vector3 handLocal = hand != null ? hammer.InverseTransformPoint(hand.position) : b.center;
            Vector3 endA = b.center + along * half;
            Vector3 endB = b.center - along * half;
            Vector3 head = (endA - handLocal).sqrMagnitude > (endB - handLocal).sqrMagnitude ? endA : endB;
            GameObject point = new GameObject("HammerHead");
            point.transform.SetParent(hammer, false);
            point.transform.localPosition = Vector3.Lerp(head, b.center, 0.12f);
            return point.transform;
        }

        void UpdateAttackEffects()
        {
            if (hammerHead == null)
                return;
            float speed = (hammerHead.position - lastHeadPosition).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            lastHeadPosition = hammerHead.position;

            bool swinging = false;
            if (boss.currentAttack >= 0)
            {
                BossAttack attack = boss.profile.attacks[boss.currentAttack];
                float t = Time.time - attackStartTime;
                swinging = t >= attack.windup - 0.15f && t <= attack.windup + attack.activeTime + 0.05f && speed > trailSpeed;
            }
            hammerTrail.emitting = swinging;

            if (boss.state == BossState.Active && lastState != BossState.Active && boss.currentAttack >= 0)
                OnAttackLands(boss.profile.attacks[boss.currentAttack]);
            lastState = boss.state;
        }

        void OnAttackLands(BossAttack attack)
        {
            if (attack.kind == BossAttackKind.Throw)
                return;
            if (attack.halfAngle >= 180)
            {
                CombatFx.AirRing(FloorUnder(transform.position), attack.innerRadius, attack.outerRadius, 0.3f + attack.outerRadius * 0.012f);
                return;
            }
            Vector3 floor = FloorUnder(hammerHead.position);
            if (hammerHead.position.y - floor.y < dustHeight)
                CombatFx.Dust(floor, attack.kind == BossAttackKind.Leap ? 4f : 3f);
        }

        Vector3 FloorUnder(Vector3 point)
        {
            RaycastHit hit;
            if (Physics.Raycast(point + Vector3.up * 0.5f, Vector3.down, out hit, 30f, ~0, QueryTriggerInteraction.Ignore))
                return hit.point;
            return point;
        }

        // update color and floor shape every frame
        void Update()
        {
            if (damageFeedback == null || !damageFeedback.isFlashing)
                body.material.color = GetBodyColor();
            UpdateAttackEffects();

            bool attacking = boss.state == BossState.Windup || boss.state == BossState.Active;
            telegraph.SetActive(showAttackArea && attacking && boss.currentAttack >= 0);
            if (!telegraph.activeSelf)
                return;

            if (meshBuiltForAttack != boss.currentAttack)
            {
                BossAttack attack = boss.profile.attacks[boss.currentAttack];
                telegraphMesh.mesh = BuildSectorMesh(attack.innerRadius, attack.outerRadius, Mathf.Min(attack.HalfRange(), 180));
                meshBuiltForAttack = boss.currentAttack;
            }

            BossAttack current = boss.profile.attacks[boss.currentAttack];
            telegraph.transform.position = new Vector3(transform.position.x, 0.05f, transform.position.z);
            telegraph.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y + current.AimAngle(), 0);
            telegraphRenderer.material.color = boss.state == BossState.Active ? activeAreaColor : windupAreaColor;
        }

        // color for the current phase
        Color GetBodyColor()
        {
            if (boss.state == BossState.Windup)
                return windupColor;
            if (boss.state == BossState.Active)
                return activeColor;
            if (boss.state == BossState.Recovery)
                return recoveryColor;
            return normalColor;
        }

        // Adapted from: Anthropic (2026) Claude [Large language model].
        // Prompt: "floor flat ring sector mesh for boss attack indicator"
        // Accessed: 5 October 2026. Modified by P. Filippenko.
        static Mesh BuildSectorMesh(float innerRadius, float outerRadius, float halfAngle)
        {
            int segments = 40;
            Vector3[] vertices = new Vector3[(segments + 1) * 2];
            for (int i = 0; i <= segments; i++)
            {
                float angle = (-halfAngle + 2 * halfAngle * i / segments) * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                vertices[i * 2] = direction * innerRadius;
                vertices[i * 2 + 1] = direction * outerRadius;
            }

            int[] triangles = new int[segments * 12];
            for (int i = 0; i < segments; i++)
            {
                int v = i * 2;
                int t = i * 12;
                triangles[t + 0] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 3;
                triangles[t + 3] = v; triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
                triangles[t + 6] = v; triangles[t + 7] = v + 3; triangles[t + 8] = v + 1;
                triangles[t + 9] = v; triangles[t + 10] = v + 2; triangles[t + 11] = v + 3;
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
