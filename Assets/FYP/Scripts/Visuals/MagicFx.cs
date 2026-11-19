using UnityEngine;

namespace FYP
{
    public static class MagicFx
    {
        public static Material material;
        public const float WhiteShare = 0.1f;

        public static ParticleSystem.MinMaxGradient Colors()
        {
            Gradient g = new Gradient();
            g.mode = GradientMode.Fixed;
            g.SetKeys(new[] { new GradientColorKey(Color.black, 1 - WhiteShare), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            ParticleSystem.MinMaxGradient colors = new ParticleSystem.MinMaxGradient(g);
            colors.mode = ParticleSystemGradientMode.RandomColor;
            return colors;
        }

        public static ParticleSystem Make(string name, Vector3 position, Transform parent, float duration, bool loop, bool local, ParticleSystem.MinMaxGradient? colors = null)
        {
            GameObject go = new GameObject(name);
            go.transform.position = position;
            if (parent != null)
                go.transform.SetParent(parent, true);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = duration;
            main.loop = loop;
            main.startColor = colors ?? Colors();
            main.simulationSpace = local ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.maxParticles = 600;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;

            Gradient fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                         new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.6f), new GradientAlphaKey(0, 1) });
            var color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = fade;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0.2f));

            go.GetComponent<ParticleSystemRenderer>().material = material;
            return ps;
        }

        public static ParticleSystem Core(Transform ball)
        {
            ParticleSystem ps = Make("MagicCore", ball.position, ball, 1, true, true);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.34f);
            main.startSpeed = 0;
            var shape = ps.shape;
            shape.radius = 0.22f;
            var emission = ps.emission;
            emission.rateOverTime = 320;
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = 0;
            velocity.y = 0;
            velocity.z = 0;
            velocity.orbitalX = 5;
            velocity.orbitalY = 8;
            velocity.orbitalZ = 4;
            velocity.radial = -0.6f;
            ps.Play();
            return ps;
        }

        public static ParticleSystem Trail(Transform ball)
        {
            ParticleSystem ps = Make("MagicTrail", ball.position, ball, 1, true, false);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.32f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
            var shape = ps.shape;
            shape.radius = 0.18f;
            var emission = ps.emission;
            emission.rateOverTime = 60;
            emission.rateOverDistance = 30;
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 2f;
            ps.Play();
            return ps;
        }

        public static void Burst(Vector3 position, int count, float speed, float size)
        {
            ParticleSystem ps = Make("MagicBurst", position, null, 0.6f, false, false);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.3f, size);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.2f, speed);
            main.gravityModifier = 0.4f;
            var shape = ps.shape;
            shape.radius = 0.2f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)count) });
            ps.Play();
        }

        public static void Gather(Transform hand, float duration, float radius)
        {
            ParticleSystem ps = Make("MagicGather", hand.position, hand, duration, false, true);
            var main = ps.main;
            main.startLifetime = 0.35f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.14f);
            main.startSpeed = -radius / 0.35f;
            var shape = ps.shape;
            shape.radius = radius;
            shape.radiusThickness = 0;
            var emission = ps.emission;
            emission.rateOverTime = 90;
            ps.Play();
        }

        public static void GroundBurst(Vector3 floor, float radius)
        {
            ParticleSystem ring = Make("MagicGroundRing", floor + Vector3.up * 0.15f, null, 0.7f, false, false);
            var main = ring.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(radius * 2f, radius * 3.2f);
            var shape = ring.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.4f;
            shape.rotation = new Vector3(90, 0, 0);
            var emission = ring.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 420) });
            ring.Play();

            ParticleSystem cloud = Make("MagicGroundCloud", floor + Vector3.up * 0.3f, null, 1.2f, false, false);
            main = cloud.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, radius * 1.2f);
            shape = cloud.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.6f;
            shape.rotation = new Vector3(-90, 0, 0);
            emission = cloud.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 140) });
            cloud.Play();
        }
    }
}
