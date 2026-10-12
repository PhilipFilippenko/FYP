using UnityEngine;

namespace FYP
{
    // boss colors per phase and the attack area drawn on the floor
    public class BossVisuals : MonoBehaviour
    {
        public Material telegraphMaterial;

        [Header("Body colors")]
        public Color windupColor = new Color(1f, 0.55f, 0.1f);
        public Color activeColor = new Color(1f, 0.1f, 0.1f);
        public Color recoveryColor = new Color(0.4f, 0.4f, 0.45f);

        [Header("Floor area colors")]
        public Color windupAreaColor = new Color(1f, 0.6f, 0.1f, 0.25f);
        public Color activeAreaColor = new Color(1f, 0.1f, 0.1f, 0.55f);

        BossController boss;
        DamageFeedback damageFeedback;
        Renderer body;
        Color normalColor;

        GameObject telegraph;
        MeshFilter telegraphMesh;
        Renderer telegraphRenderer;
        int meshBuiltForAttack = -1;

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
        }

        // update color and floor shape every frame
        void Update()
        {
            if (damageFeedback == null || !damageFeedback.isFlashing)
                body.material.color = GetBodyColor();

            bool attacking = boss.state == BossState.Windup || boss.state == BossState.Active;
            telegraph.SetActive(attacking && boss.currentAttack >= 0);
            if (!telegraph.activeSelf)
                return;

            if (meshBuiltForAttack != boss.currentAttack)
            {
                BossAttack attack = boss.profile.attacks[boss.currentAttack];
                telegraphMesh.mesh = BuildSectorMesh(attack.innerRadius, attack.outerRadius, attack.halfAngle);
                meshBuiltForAttack = boss.currentAttack;
            }

            telegraph.transform.position = new Vector3(transform.position.x, 0.05f, transform.position.z);
            telegraph.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
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

        // flat ring piece on the floor, facing forward like the boss
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

            // draw both sides, so the shape is visible from above and below
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
