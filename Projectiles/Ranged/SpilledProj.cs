﻿using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace EmptySet.Projectiles.Ranged;

/// <summary>
/// 溢能弹幕
/// </summary>
public class SpilledProj : ModProjectile
{
    public override void SetDefaults()
    {
        Projectile.width = 30;
        Projectile.height = 22;
        Projectile.scale = 0.9f;
        Projectile.penetrate = 1;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.timeLeft = 6 * EmptySet.Frame;
        Projectile.light = 0.35f;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        for (int i = 0; i < 15; i++)
        SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
        {
            Projectile projectile = Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), Projectile.position, Vector2.Zero,
                ModContent.ProjectileType<SpilledExplode>(), (int)Projectile.damage, 1f, Projectile.owner);
            projectile.Center = Projectile.Center;
        }
    }

    public override void AI()
    {
        var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.NorthPole);
        dust.scale = 1.5f;
        dust.noGravity = true;
        var red = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Flare_Blue);
        red.scale = 1.5f;
        red.noGravity = true;
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(0f);
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
        for (int i = 0; i < 15; i++)
            Dust.NewDust(Projectile.Center + Projectile.velocity, Projectile.width, Projectile.height, DustID.NorthPole, Projectile.velocity.X, Projectile.velocity.Y, 255, default(Color), 1.2f);
        return base.OnTileCollide(oldVelocity);
    }
}
public class SpilledExplode : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_0";
    public override void SetDefaults()
    {
        Projectile.width = 72;
        Projectile.height = 72;
        Projectile.friendly = true;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.light = 1f;
        Projectile.timeLeft = 9;
    }
    public override void AI()
    {
        for (int i = 0; i < 5; i++)
        {

            float num1 = Main.rand.Next(-36, 36);
            float num2 = Main.rand.Next(-36, 36);
            float num3 = (float)Math.Sqrt(num1 * num1 + num2 * num2);
            num3 = Main.rand.Next(4, 12) / num3;
            num1 *= num3;
            num2 *= num3;
            var yellow = Dust.NewDustDirect(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.Flare_Blue, Scale: Main.rand.NextFloat(2f, 3f));
            yellow.noGravity = true;
            var blue = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.NorthPole);
            blue.noGravity = true;
            yellow.position = Projectile.Center;
            yellow.position += new Vector2((float)Main.rand.Next(-5, 6), (float)Main.rand.Next(-5, 6));
            yellow.velocity.X = num1;
            yellow.velocity.Y = num2;
        }
    }
}
