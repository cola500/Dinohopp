using UnityEditor;
using UnityEngine;

/// <summary>
/// Procedurally rasterises the dino sprites used by the Animator. Six poses share one
/// drawing routine — pose-specific parameters (leg lift, tail angle, eye state) come
/// from <see cref="GetPoseParams"/> so tweaking one shape touches every pose at once.
///
/// To iterate on the dino's look: edit the constants in the "Tunables" region, run
/// <c>Tools/Dinohopp/Build Dino Sprites</c>, then run the scene builder so the
/// SpriteRenderer's default sprite picks up the change.
/// </summary>
public static class DinohoppDinoSpriteBuilder
{
    public const string SpriteDir = "Assets/Sprites/Dino";

    // ---------- Tunables (Johan: tweak here) ----------

    // Canvas size and where the dino "stands" inside it. 384 px gives crisp detail
    // even at WebGL 1080p; PPU 256 maps that to ~1.5 world units (slightly bigger
    // than the 1.33-tall collider so tail / snout / spikes can extend beyond it).
    const int   Canvas = 384;
    const float PPU    = 256f;
    static readonly Vector2 Pivot = new Vector2(0.5f, 0.10f); // feet rest near canvas bottom

    // ---- Palette: soft Nintendo / storybook ----
    static readonly Color BodyLight   = Hex("#7BC76B"); // main body green
    static readonly Color BodyShade   = Hex("#4D9A3E"); // back / shaded side
    static readonly Color BodyDeep    = Hex("#2F6B25"); // back-leg, spike base
    static readonly Color Outline     = Hex("#1F3A18"); // dark green-brown outline
    static readonly Color Belly       = Hex("#FBE9B6"); // cream belly
    static readonly Color BellyShade  = Hex("#E8D29A"); // belly separation
    static readonly Color Cheek       = Hex("#FFB5B5"); // soft pink blush
    static readonly Color EyeWhite    = Color.white;
    static readonly Color EyePupil    = Hex("#1A2540"); // near-black with blue tint
    static readonly Color Tooth       = Hex("#FFF8E0"); // off-white
    static readonly Color MouthLine   = Hex("#2A1818"); // smile line
    static readonly Color NostrilDark = Hex("#1F3A18");

    // Body anchor — most parts are positioned relative to this so the whole dino
    // can be nudged sideways/up in one place if the silhouette feels off-balance.
    const float BodyCx = 175f;
    const float BodyCy = 165f;

    // ---------- Entry ----------

    public enum Pose { Idle, IdleBlink, RunA, RunB, Jump, Fall }

    [MenuItem("Tools/Dinohopp/Build Dino Sprites")]
    public static void BuildAll()
    {
        EnsureDir();
        BuildOne(Pose.Idle,      "Dino_Idle.png");
        BuildOne(Pose.IdleBlink, "Dino_Idle_Blink.png");
        BuildOne(Pose.RunA,      "Dino_Run_A.png");
        BuildOne(Pose.RunB,      "Dino_Run_B.png");
        BuildOne(Pose.Jump,      "Dino_Jump.png");
        BuildOne(Pose.Fall,      "Dino_Fall.png");
        AssetDatabase.Refresh();
        Debug.Log($"[Dinohopp] Dino sprites built → {SpriteDir}");
    }

    static void EnsureDir()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            AssetDatabase.CreateFolder("Assets", "Sprites");
        if (!AssetDatabase.IsValidFolder(SpriteDir))
            AssetDatabase.CreateFolder("Assets/Sprites", "Dino");
    }

    static void BuildOne(Pose pose, string filename)
    {
        var canvas = new SpriteCanvas(Canvas, Canvas);
        DrawDino(canvas, pose);
        canvas.SaveAsSprite($"{SpriteDir}/{filename}", PPU, Pivot);
    }

    // ---------- Pose parameters ----------

    struct PoseParams
    {
        public float frontLegLift;    // px above the ground
        public float frontLegFwd;     // px forward (+) / back (-)
        public float backLegLift;
        public float backLegFwd;
        public float tailRot;         // degrees, 0 = horizontal-back, + = up
        public float armRot;          // degrees
        public float headY;           // head offset Y
        public bool  eyesClosed;
        public float bodyStretchY;    // 1 = neutral, >1 = taller, <1 = squashed
    }

    static PoseParams GetPoseParams(Pose pose)
    {
        switch (pose)
        {
            case Pose.Idle:
                return new PoseParams {
                    tailRot = 8f, armRot = -8f, bodyStretchY = 1f,
                };

            case Pose.IdleBlink:
                return new PoseParams {
                    tailRot = 8f, armRot = -8f, bodyStretchY = 1f,
                    eyesClosed = true,
                };

            case Pose.RunA:
                return new PoseParams {
                    frontLegLift = 30f, frontLegFwd = 18f,
                    backLegLift  = 0f,  backLegFwd  = -18f,
                    tailRot = 22f, armRot = -28f,
                    headY = 2f, bodyStretchY = 0.99f,
                };

            case Pose.RunB:
                return new PoseParams {
                    frontLegLift = 0f,  frontLegFwd = -18f,
                    backLegLift  = 30f, backLegFwd  = 18f,
                    tailRot = -2f, armRot = 14f,
                    headY = -2f, bodyStretchY = 0.99f,
                };

            case Pose.Jump:
                return new PoseParams {
                    frontLegLift = 36f, frontLegFwd = 4f,
                    backLegLift  = 36f, backLegFwd  = -4f,
                    tailRot = -20f, armRot = -38f,
                    headY = 6f, bodyStretchY = 1.08f,
                };

            case Pose.Fall:
                return new PoseParams {
                    frontLegLift = 22f, frontLegFwd = 14f,
                    backLegLift  = 22f, backLegFwd  = -14f,
                    tailRot = 38f, armRot = 22f,
                    headY = -4f, bodyStretchY = 0.94f,
                };
        }
        return default;
    }

    // ---------- Drawing ----------

    static void DrawDino(SpriteCanvas c, Pose pose)
    {
        var p = GetPoseParams(pose);

        // Back-to-front layering. Each helper is pure rasterisation; pose values are
        // applied here so the helpers stay readable.

        DrawTail(c, p);
        DrawBackLeg(c, p);
        DrawBody(c, p);
        DrawBelly(c, p);
        DrawBackSpikes(c, p);
        DrawHead(c, p);
        DrawMouth(c, p);
        DrawCheek(c, p);
        DrawEye(c, p);
        DrawNostril(c, p);
        DrawTinyArm(c, p);
        DrawFrontLeg(c, p);
    }

    static void DrawBody(SpriteCanvas c, PoseParams p)
    {
        float cx = BodyCx;
        float cy = BodyCy;
        float rx = 95f;
        float ry = 85f * p.bodyStretchY;

        // Soft drop shadow under the body — sells the "sitting on ground" read.
        c.FillEllipse(BodyCx + 5f, 38f, 85f, 9f, new Color(0f, 0f, 0f, 0.20f));

        // Outline (slightly larger ellipse in dark green).
        c.FillEllipse(cx, cy, rx + 4f, ry + 4f, Outline);

        // Body fill — main green.
        c.FillEllipse(cx, cy, rx, ry, BodyLight);

        // Top-back shading: darker green crescent on the upper-left half.
        // Drawn as a darker ellipse offset up-left, masked to the body by drawing the
        // belly highlight afterwards.
        c.FillEllipse(cx - 18f, cy + 14f, rx * 0.85f, ry * 0.70f, BodyShade);
    }

    static void DrawBelly(SpriteCanvas c, PoseParams p)
    {
        // Big soft cream oval in the lower-front of the body. Slight darker rim
        // gives a subtle separation from the green without needing a hard line.
        c.FillEllipse(BodyCx + 22f, BodyCy - 22f, 62f, 50f, BellyShade);
        c.FillEllipse(BodyCx + 24f, BodyCy - 20f, 56f, 44f, Belly);
    }

    static void DrawHead(SpriteCanvas c, PoseParams p)
    {
        // Head is a big rounded block; snout extends to the right.
        float hcx = BodyCx + 70f;
        float hcy = BodyCy + 80f + p.headY;

        // Outline halo.
        c.FillEllipse(hcx, hcy, 78f, 70f, Outline);
        // Snout outline — separate ellipse jutting forward.
        c.FillEllipse(hcx + 55f, hcy - 8f, 48f, 36f, Outline);

        // Fills.
        c.FillEllipse(hcx, hcy, 74f, 66f, BodyLight);
        c.FillEllipse(hcx + 55f, hcy - 8f, 44f, 32f, BodyLight);

        // Top-back shade on the head crown.
        c.FillEllipse(hcx - 12f, hcy + 18f, 60f, 38f, BodyShade);

        // Light belly continuation up onto the lower jaw — keeps the cream
        // running from chin to belly visually.
        c.FillEllipse(hcx + 35f, hcy - 24f, 48f, 18f, Belly);
    }

    static void DrawMouth(SpriteCanvas c, PoseParams p)
    {
        float hcx = BodyCx + 70f;
        float hcy = BodyCy + 80f + p.headY;

        // Tiny smile line — capsule arc approximated by two short lines.
        var a = new Vector2(hcx + 78f, hcy - 16f);
        var b = new Vector2(hcx + 92f, hcy - 20f);
        var d = new Vector2(hcx + 105f, hcy - 14f);
        c.DrawLine(a, b, 2.5f, MouthLine);
        c.DrawLine(b, d, 2.5f, MouthLine);

        // One little tooth peeking out — sells the friendly T-Rex without looking scary.
        c.FillRoundedRect(hcx + 88f, hcy - 22f, 5f, 7f, 1.5f, Tooth);
    }

    static void DrawCheek(SpriteCanvas c, PoseParams p)
    {
        float hcx = BodyCx + 70f;
        float hcy = BodyCy + 80f + p.headY;
        // Soft pink blush below the eye.
        c.FillEllipse(hcx + 30f, hcy - 8f, 14f, 8f, new Color(Cheek.r, Cheek.g, Cheek.b, 0.65f));
    }

    static void DrawEye(SpriteCanvas c, PoseParams p)
    {
        float hcx = BodyCx + 70f;
        float hcy = BodyCy + 80f + p.headY;
        float ex = hcx + 28f;
        float ey = hcy + 12f;

        if (p.eyesClosed)
        {
            // Closed eye: thick curved line (small arc using two segments).
            var l1 = new Vector2(ex - 14f, ey - 1f);
            var l2 = new Vector2(ex,        ey + 4f);
            var l3 = new Vector2(ex + 14f, ey - 1f);
            c.DrawLine(l1, l2, 3.5f, Outline);
            c.DrawLine(l2, l3, 3.5f, Outline);
            // Tiny eyelash curl for a happy feel.
            c.DrawLine(new Vector2(ex - 14f, ey - 1f), new Vector2(ex - 18f, ey + 3f), 2f, Outline);
            return;
        }

        // Open eye: dark outline → white sclera → pupil → highlight.
        c.FillCircle(ex, ey, 17f, Outline);
        c.FillCircle(ex, ey, 14f, EyeWhite);
        c.FillCircle(ex + 3f, ey - 1f, 9f, EyePupil);
        c.FillCircle(ex + 5f, ey + 3f, 3.5f, EyeWhite); // sparkle
        c.FillCircle(ex - 1f, ey - 4f, 1.5f, EyeWhite); // small secondary glint
    }

    static void DrawNostril(SpriteCanvas c, PoseParams p)
    {
        float hcx = BodyCx + 70f;
        float hcy = BodyCy + 80f + p.headY;
        // Tiny dot near snout tip.
        c.FillCircle(hcx + 96f, hcy + 0f, 2.4f, NostrilDark);
    }

    static void DrawBackSpikes(SpriteCanvas c, PoseParams p)
    {
        // Four little rounded triangles running along the back ridge from
        // base of skull down to base of tail. Slightly darker than body for definition.
        DrawSpike(c, BodyCx + 35f, BodyCy + 75f, 14f, BodyDeep);
        DrawSpike(c, BodyCx + 5f,  BodyCy + 82f, 14f, BodyDeep);
        DrawSpike(c, BodyCx - 25f, BodyCy + 78f, 13f, BodyDeep);
        DrawSpike(c, BodyCx - 55f, BodyCy + 65f, 11f, BodyDeep);
    }

    static void DrawSpike(SpriteCanvas c, float baseCx, float baseCy, float size, Color color)
    {
        // Soft triangle with a hint of outline by drawing slightly bigger underneath.
        c.FillTriangle(
            new Vector2(baseCx - size, baseCy),
            new Vector2(baseCx + size, baseCy),
            new Vector2(baseCx,        baseCy + size * 1.6f),
            Outline);
        c.FillTriangle(
            new Vector2(baseCx - size + 2f, baseCy + 1f),
            new Vector2(baseCx + size - 2f, baseCy + 1f),
            new Vector2(baseCx,             baseCy + size * 1.6f - 3f),
            color);
    }

    static void DrawTail(SpriteCanvas c, PoseParams p)
    {
        // Tail rendered as a chain of overlapping circles that taper from a thick
        // base to a fine tip. Curved into an upward sweep (positive y) which reads
        // as a perky cartoon T-Rex tail. tailRot rotates the whole sweep around the
        // base — small for idle, bigger for run/jump.
        var basePos = new Vector2(BodyCx - 70f, BodyCy + 8f);

        const int Segments = 12;
        float baseRadius = 38f;
        float length = 140f;

        // Pre-compute a curved path: starts pointing back-left, gently curls upward.
        // Without rotation: each segment steps along an arc tilted by tailRot.
        var pts = new Vector2[Segments + 1];
        for (int i = 0; i <= Segments; i++)
        {
            float t = i / (float)Segments; // 0..1 along tail
            float curve = Mathf.Sin(t * Mathf.PI * 0.55f); // gentle upward arc
            float baseAng = (180f + p.tailRot) * Mathf.Deg2Rad;
            // Offset perpendicular to base angle for the curve.
            Vector2 along = new Vector2(Mathf.Cos(baseAng), Mathf.Sin(baseAng));
            Vector2 perp  = new Vector2(-along.y, along.x);
            pts[i] = basePos + along * (length * t) + perp * (curve * 18f);
        }

        // Outline pass: slightly bigger circles in dark green.
        for (int i = 0; i <= Segments; i++)
        {
            float t = i / (float)Segments;
            float r = Mathf.Lerp(baseRadius + 4f, 4f, t); // taper outline
            c.FillCircle(pts[i].x, pts[i].y, r, Outline);
        }
        // Fill pass: light green circles slightly smaller, on top.
        for (int i = 0; i <= Segments; i++)
        {
            float t = i / (float)Segments;
            float r = Mathf.Lerp(baseRadius, 1.5f, t);
            c.FillCircle(pts[i].x, pts[i].y, r, BodyLight);
        }
        // Subtle shade ridge along the top half of the tail base — sells volume.
        for (int i = 0; i <= Segments / 2; i++)
        {
            float t = i / (float)Segments;
            float r = Mathf.Lerp(baseRadius * 0.55f, 4f, t * 2f);
            c.FillCircle(pts[i].x, pts[i].y + 8f, r, BodyShade);
        }
    }

    static void DrawBackLeg(SpriteCanvas c, PoseParams p)
    {
        // Back leg sits behind the body — darker shade so it reads as background.
        float legX = BodyCx - 20f + p.backLegFwd;
        float legY = 90f + p.backLegLift;
        DrawLeg(c, legX, legY, BodyDeep, isBack: true);
    }

    static void DrawFrontLeg(SpriteCanvas c, PoseParams p)
    {
        // Front leg in foreground — main body color.
        float legX = BodyCx + 35f + p.frontLegFwd;
        float legY = 90f + p.frontLegLift;
        DrawLeg(c, legX, legY, BodyLight, isBack: false);
    }

    static void DrawLeg(SpriteCanvas c, float hipX, float hipY, Color color, bool isBack)
    {
        // Hip → ankle capsule for the leg itself.
        float ankleX = hipX;
        float ankleY = 56f; // foot top
        var hip   = new Vector2(hipX, hipY);
        var ankle = new Vector2(ankleX, ankleY);

        // Outline first (thicker, dark) then fill.
        c.DrawLine(hip, ankle, 38f, Outline);
        c.DrawLine(hip, ankle, 32f, color);

        // Foot: a chunky rounded rectangle extending forward (to the right).
        float footW = 44f;
        float footH = 18f;
        float footX = ankleX - 8f;
        float footY = 44f; // bottom of foot
        c.FillRoundedRect(footX - 2f, footY - 2f, footW + 4f, footH + 4f, 8f, Outline);
        c.FillRoundedRect(footX, footY, footW, footH, 7f, color);

        // Three tiny claw bumps on the front of the foot — only on visible foot.
        if (!isBack)
        {
            float clawY = footY + footH * 0.5f;
            for (int i = 0; i < 3; i++)
            {
                float cxClaw = footX + footW - 4f - i * 7f;
                c.FillCircle(cxClaw, clawY - 6f, 2.6f, Outline);
            }
        }
    }

    static void DrawTinyArm(SpriteCanvas c, PoseParams p)
    {
        // T-Rex arms — very small, hanging from upper chest area.
        var shoulder = new Vector2(BodyCx + 60f, BodyCy + 10f);
        float ang = (-90f + p.armRot) * Mathf.Deg2Rad;
        var hand = shoulder + new Vector2(Mathf.Cos(ang) * 28f, Mathf.Sin(ang) * 28f);

        c.DrawLine(shoulder, hand, 13f, Outline);
        c.DrawLine(shoulder, hand, 9f,  BodyLight);

        // Two tiny fingers as little blobs at the hand end.
        c.FillCircle(hand.x + 2f, hand.y - 2f, 3f, Outline);
        c.FillCircle(hand.x - 3f, hand.y + 1f, 3f, Outline);
        c.FillCircle(hand.x + 2f, hand.y - 2f, 2f, BodyLight);
        c.FillCircle(hand.x - 3f, hand.y + 1f, 2f, BodyLight);
    }

    // ---------- Tiny helpers ----------

    static Color Hex(string hex)
    {
        if (hex[0] == '#') hex = hex.Substring(1);
        byte r = System.Convert.ToByte(hex.Substring(0, 2), 16);
        byte g = System.Convert.ToByte(hex.Substring(2, 2), 16);
        byte b = System.Convert.ToByte(hex.Substring(4, 2), 16);
        return new Color32(r, g, b, 255);
    }
}
