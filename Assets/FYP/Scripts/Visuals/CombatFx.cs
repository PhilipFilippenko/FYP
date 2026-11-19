using UnityEngine;

namespace FYP
{
    public static class CombatFx
    {
        static Material trailMaterial;

        static Material TrailMaterial()
        {
            if (trailMaterial != null)
                return trailMaterial;
            Texture2D band = new Texture2D(1, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++)
                band.SetPixel(0, y, new Color(1, 1, 1, Mathf.Sin(Mathf.PI * (y + 0.5f) / 32)));
            band.wrapMode = TextureWrapMode.Clamp;
            band.Apply();
            trailMaterial = new Material(MagicFx.material);
            trailMaterial.SetTexture("_BaseMap", band);
            return trailMaterial;
        }

        static ParticleSystem.MinMaxGradient Between(Color a, Color b)
        {
            return new ParticleSystem.MinMaxGradient(a, b);
        }

        public static void Blood(Vector3 point, Vector3 towardAttacker, float damage)
        {
            ParticleSystem.MinMaxGradient red = Between(new Color(0.28f, 0.01f, 0.01f), new Color(0.55f, 0.04f, 0.03f));
            ParticleSystem spray = MagicFx.Make("BloodSpray", point, null, 1f, false, false, red);
            spray.transform.rotation = Quaternion.LookRotation(Vector3.Lerp(towardAttacker, Vector3.up, 0.35f));
            var main = spray.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.95f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            main.gravityModifier = 2.2f;
            var shape = spray.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35;
            shape.radius = 0.08f;
            var emission = spray.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)Mathf.Clamp(30 + damage * 1.5f, 30, 140)) });
            spray.Play();

            ParticleSystem mist = MagicFx.Make("BloodMist", point + towardAttacker * 0.15f, null, 0.5f, false, false, Between(new Color(0.35f, 0.02f, 0.02f, 0.6f), new Color(0.5f, 0.05f, 0.04f, 0.4f)));
            main = mist.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            shape = mist.shape;
            shape.radius = 0.15f;
            emission = mist.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)Mathf.Clamp(6 + damage * 0.2f, 6, 20)) });
            mist.Play();

            RaycastHit hit;
            if (!Physics.Raycast(point + towardAttacker * 0.4f + Vector3.up * 0.2f, Vector3.down, out hit, 10f, ~0, QueryTriggerInteraction.Ignore))
                return;
            ParticleSystem splats = MagicFx.Make("BloodSplat", hit.point + Vector3.up * 0.02f + towardAttacker * 0.5f, null, 6f, false, false, Between(new Color(0.18f, 0.01f, 0.01f), new Color(0.32f, 0.02f, 0.02f)));
            main = splats.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.7f);
            main.startSpeed = 0;
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            shape = splats.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.8f;
            shape.rotation = new Vector3(90, 0, 0);
            var size = splats.sizeOverLifetime;
            size.enabled = false;
            emission = splats.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)Mathf.Clamp(3 + damage * 0.15f, 3, 12)) });
            splats.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            splats.Play();
        }

        public static void Dust(Vector3 floor, float radius)
        {
            ParticleSystem.MinMaxGradient dust = Between(new Color(0.33f, 0.29f, 0.24f), new Color(0.55f, 0.5f, 0.43f));
            ParticleSystem ring = MagicFx.Make("ImpactDust", floor + Vector3.up * 0.2f, null, 1.2f, false, false, dust);
            var main = ring.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(radius * 1.2f, radius * 2.2f);
            var shape = ring.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius * 0.2f;
            shape.rotation = new Vector3(90, 0, 0);
            var emission = ring.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)Mathf.Clamp(radius * 30, 40, 220)) });
            var limit = ring.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = radius * 0.4f;
            limit.dampen = 0.15f;
            ring.Play();

            ParticleSystem cloud = MagicFx.Make("ImpactCloud", floor + Vector3.up * 0.4f, null, 1.6f, false, false, dust);
            main = cloud.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(radius * 0.3f, radius * 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
            shape = cloud.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = radius * 0.4f;
            shape.rotation = new Vector3(-90, 0, 0);
            emission = cloud.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 25) });
            cloud.Play();
        }

        public static void AirRing(Vector3 center, float fromRadius, float toRadius, float time)
        {
            ParticleSystem ps = MagicFx.Make("AirRing", center + Vector3.up * 0.4f, null, time, false, false, Between(new Color(1, 1, 1, 0.35f), new Color(0.85f, 0.85f, 0.85f, 0.2f)));
            var main = ps.main;
            main.startLifetime = time;
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            main.startSpeed = (toRadius - fromRadius) / time;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = Mathf.Max(fromRadius, 0.1f);
            shape.radiusThickness = 0;
            shape.rotation = new Vector3(90, 0, 0);
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)Mathf.Clamp(toRadius * 14, 60, 400)) });
            ps.Play();
        }

        public static TrailRenderer Trail(Transform at, float width, float time, bool flat)
        {
            Vector3 s = at.lossyScale;
            at.localScale = new Vector3(at.localScale.x / s.x, at.localScale.y / s.y, at.localScale.z / s.z);
            TrailRenderer trail = at.gameObject.AddComponent<TrailRenderer>();
            trail.alignment = flat ? LineAlignment.TransformZ : LineAlignment.View;
            trail.widthMultiplier = width;
            trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0.4f);
            trail.time = time;
            trail.minVertexDistance = 0.04f;
            trail.numCornerVertices = 2;
            trail.emitting = false;
            trail.material = TrailMaterial();
            trail.textureMode = LineTextureMode.Stretch;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            Gradient fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(0.8f, 0.82f, 0.85f), 1) },
                         new[] { new GradientAlphaKey(0.9f, 0), new GradientAlphaKey(0.35f, 0.4f), new GradientAlphaKey(0, 1) });
            trail.colorGradient = fade;
            return trail;
        }
    }
}
