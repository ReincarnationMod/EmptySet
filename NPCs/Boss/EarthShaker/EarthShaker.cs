﻿using Microsoft.Xna.Framework;
using EmptySet.Items.Accessories;
using EmptySet.Items.Consumables;
using EmptySet.Items.Materials;
using EmptySet.Projectiles.Boss;
using EmptySet.Utils;
using System;
using EmptySet.Common.Abstract.NPCs;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using EmptySet.Common.ItemDropRules;
using EmptySet.Common.Systems;
using EmptySet.Projectiles.Boss.EarthShaker;

namespace EmptySet.NPCs.Boss.EarthShaker
{
	[AutoloadBossHead]
	class EarthShaker: NPCStateMachine
	{
		Player player;

		float velocityChangeX = 0.1f;
		float velocityChangeY = 0.15f;

		public static int secondStageHeadSlot = -1;

		bool x = false;
		bool y = false;

		public override void Initialize()
		{
			//注册npc状态
			RegisterState(new SpawnState());
			RegisterState(new NormalState());
			RegisterState(new AttackState());
			RegisterState(new RageAttackState());
			RegisterState(new RunState());
			//注册npc攻击状态
			RegisterState(new AttackState1());
			RegisterState(new AttackState2());
			RegisterState(new AttackState3());
			RegisterState(new AttackState4());
			RegisterState(new AttackState5());
		}
		public override void SetStaticDefaults()
		{
			// DisplayName.SetDefault("大地震撼者");

			// 如果boss有召唤道具，需要相应的代码
			NPCID.Sets.MPAllowedEnemies[Type] = true;

			var drawModifier = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
			{ 
				CustomTexturePath = "EmptySet/NPCs/Boss/EarthShaker/EarthShaker",
				PortraitScale =	0.5f,
			};
			NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, drawModifier);
			Main.npcFrameCount[NPC.type] = 29;//npc动画帧数量
		}
		public override void SetDefaults()
		{
			NPC.aiStyle = -1;
			NPC.width = 308;
			NPC.height = 380;
			NPC.defense = 8;
			NPC.damage = 45;
			NPC.lifeMax = 2200;
			NPC.boss = true;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.value = 60f;
			NPC.knockBackResist = 0f;//击退抗性0代表100%
			NPC.noGravity = true;
			NPC.Hitbox = new Rectangle(NPC.Hitbox.X, NPC.Hitbox.Y, NPC.Hitbox.Width-30, NPC.Hitbox.Height-60);
			DrawOffsetY = 15;
			DrawOffsetY = 12;
			if (!Main.dedServ)
			{
				Music = MusicLoader.GetMusicSlot(Mod, "Assets/Music/IronBlade");
			}
		}


		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{

            LeadingConditionRule notExpertRule = new LeadingConditionRule(new Conditions.NotExpert());
			notExpertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<MetalFragment>(),1,1, Main.rand.Next(30, 51)));
			notExpertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ChargedCrystal>(), 1, 1, Main.rand.Next(25, 41)));
			notExpertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<DefenderTalisman>(), 20));

			npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<EarthShakerChest>()));
			//npcLoot.Add(MasterAndNotExpertDropRule);
			//npcLoot.Add(ExpertAndNotMasterDropRule);
			npcLoot.Add(notExpertRule);
		}
		
		public int laserDamage;
		/// <summary>
		/// 充能晶体释放激光次数
		/// </summary>
		public int laserCount;
		public int halfLifeLaserDamage;
		public int missileDamage;
		public int bigShockWaveDamage;
		public int shockWaveDamage;
		public int halfLifeChargedCrystalDamage;


		public override bool PreAI()
        {
			NPC.lifeMax = Main.masterMode ? 3600 : Main.expertMode ? 2900 : 2200;
			NPC.defense = Main.masterMode ? 10 : Main.expertMode ? 9 : 8;
			NPC.damage = Main.masterMode ? 80 : Main.expertMode ? 65 : 45;
			laserDamage = Main.masterMode ? 70 : Main.expertMode ? 50 : 30;
			laserCount = Main.masterMode ? 8 : Main.expertMode ? 7 : 5;
			halfLifeLaserDamage = Main.masterMode ? 50 : Main.expertMode ? 40 : 30;
			missileDamage = Main.masterMode ? 95 : Main.expertMode ? 55 : 40;
			shockWaveDamage = Main.masterMode ? 70 : Main.expertMode ? 55 : 35;
			bigShockWaveDamage = Main.masterMode ? 70 : Main.expertMode ? 55 : 35;
			halfLifeChargedCrystalDamage = Main.masterMode ? 50 : Main.expertMode ? 40 : 30;

			if (NPC.life > NPC.lifeMax) NPC.life = NPC.lifeMax;
			return base.PreAI();
        }


		/// <summary>
		/// 特大跳
		/// </summary>
		/// <param name="n"></param>
		public void BigJump(NPCStateMachine n)
		{
			//NPC = n.NPC;
			n.Timer1++;
			if (n.Timer1 == 1)
			{
				NPC.noTileCollide = true;
			}
			else if (n.Timer1 < 100)
			{
				NPC.velocity = Vector2.Normalize(n.target.Center + new Vector2(0, -350) - NPC.Center) * 30;
				if (Math.Abs(NPC.Center.X - n.target.Center.X) < 16 && Math.Abs(n.target.Center.Y - NPC.Center.Y - 350) < 16) n.Timer1 = 100;
			}
			else if (n.Timer1 < 145)
			{
				NPC.velocity = Vector2.Zero;
			}
			else if (n.Timer1 == 145)
			{
				NPC.noTileCollide = false;
				NPC.velocity.Y = 20f;
			}
			else
			{
				if (NPC.velocity.Y == 0)
				{
					n.Timer2++;
					if (n.Timer2 == 1)
					{
						SoundEngine.PlaySound(SoundID.Item14, NPC.position);
						//落地时会在落点生成一个小冲击波（冲击波处会生成可以减速玩家的雾气）
						Projectile.NewProjectile(NPC.GetSource_FromAI(), new(NPC.Center.X + (NPC.width / 2), NPC.Center.Y + 150), new(0, 0), ModContent.ProjectileType<ShockWaveProjectile>(), EmptySetUtils.ScaledProjDamage(((EarthShaker)NPC.ModNPC).shockWaveDamage), 0, Main.myPlayer);
						Projectile.NewProjectile(NPC.GetSource_FromAI(), new(NPC.Center.X - (NPC.width / 2), NPC.Center.Y + 150), new(0, 0), ModContent.ProjectileType<ShockWaveProjectile>(), EmptySetUtils.ScaledProjDamage(((EarthShaker)NPC.ModNPC).shockWaveDamage), 0, Main.myPlayer);
						for (int i = 0; i < 7; i++)
						{
							Projectile.NewProjectile(NPC.GetSource_FromAI(), new(NPC.Center.X + (NPC.width / 2 + i * 30), NPC.Center.Y + 150), new(0, 0), ModContent.ProjectileType<SlowDownMistProj>(), EmptySetUtils.ScaledProjDamage(((EarthShaker)NPC.ModNPC).shockWaveDamage), 0, Main.myPlayer);
							Projectile.NewProjectile(NPC.GetSource_FromAI(), new(NPC.Center.X - (NPC.width / 2 + i * 30), NPC.Center.Y + 150), new(0, 0), ModContent.ProjectileType<SlowDownMistProj>(), EmptySetUtils.ScaledProjDamage(((EarthShaker)NPC.ModNPC).shockWaveDamage), 0, Main.myPlayer);
						}
					}
					if (n.Timer2 >= 30)
					{
						NPC.noTileCollide = true;
						n.Timer1 = 0;
						n.Timer2 = 0;
						n.NPCAttackStatesCount++;
					}
				}
			}
		}

		private bool moveToPosition(Vector2 vector)
		{
			x = false;
			y = false;
			if (NPC.Center.X > vector.X + 20)
			{
				NPC.velocity.X = -15;
			}
			else if (NPC.Center.X < vector.X - 20)
			{
				NPC.velocity.X = 15;
			}
			else 
			{
				NPC.velocity.X = 0;
				NPC.Center = new(vector.X, NPC.Center.Y);
				x = true;
			}
			if (NPC.Center.Y > vector.Y - 190 -16*35 + 20)
			{
				NPC.velocity.Y = -10;
			}
			else if (NPC.Center.Y < vector.Y - 190 - 16 * 35 - 20)
			{
				NPC.velocity.Y = 10;
			}
			else
			{
				NPC.velocity.Y = 0;
				NPC.Center = new(NPC.Center.X, vector.Y - 190 - 16 * 35);
				y = true;
			}
			return x && y;
		}


		private void move()
		{
			if (NPC.Center.X > player.Center.X)
			{
				if (NPC.velocity.X>=-5) 
				{
					NPC.velocity.X -= velocityChangeX;
				}
			}
			else if (NPC.Center.X < player.Center.X) 
			{
				if (NPC.velocity.X <= 5)
				{
					NPC.velocity.X += velocityChangeX;
				}
			}
			if (NPC.Center.Y > player.Center.Y-100)
			{
				if (NPC.velocity.Y >= -6)
				{
					NPC.velocity.Y -= velocityChangeY;
				}
			}
			else if (NPC.Center.Y < player.Center.Y- 100)
			{
				if (NPC.velocity.Y <= 6)
				{
					NPC.velocity.Y += velocityChangeY;
				}
			}
		}

		public override void Load()
		{
			string texture = BossHeadTexture;
			secondStageHeadSlot = Mod.AddBossHeadTexture(texture, -1);
		}
		public override void BossHeadSlot(ref int index)
		{
			if (secondStageHeadSlot != -1)
			{
				index = secondStageHeadSlot;
			}
		}
		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				//new MoonLordPortraitBackgroundProviderBestiaryInfoElement(),
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
				new FlavorTextBestiaryInfoElement("曾经用于驱赶怪物的守城机械，然而，曾经的城市或化为尘土，或被掩埋与地底，而已经彻底失控的机械依旧在执行着它的“使命”，清除着所见到的一切活物.")
			});
		}
		public override void OnKill()
        {
			NPC.SetEventFlagCleared(ref DownedBossSystem.downedEarthShaker, -1);
        }

		public override void HitEffect(NPC.HitInfo hit)
		{
			// 如果NPC死亡，刷出尸块并播放声音
			if (Main.netMode == NetmodeID.Server)
			{
				return;
			}

			if (NPC.life <= 0)
			{

				int GoreType1 = Mod.Find<ModGore>("EarthShakerGore1").Type;
				int GoreType2 = Mod.Find<ModGore>("EarthShakerGore2").Type;
				int GoreType3 = Mod.Find<ModGore>("EarthShakerGore3").Type;
				int GoreType4 = Mod.Find<ModGore>("EarthShakerGore4").Type;


				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-6, 7), Main.rand.Next(-6, 7)), GoreType1);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-6, 7), Main.rand.Next(-6, 7)), GoreType2);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-6, 7), Main.rand.Next(-6, 7)), GoreType3);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-6, 7), Main.rand.Next(-6, 7)), GoreType4);
				
			}
		}
		public void CreateFakeNPC(Vector2 pos) 
		{

		}
    }
	/// <summary>
	/// 用npc draw 函数代替 假npc
	/// </summary>
	class FakeNPC :ModNPC
	{
		public override string Texture => "EmptySet/NPCs/Boss/EarthShaker/EarthShaker";
		public override void SetStaticDefaults()
		{
			// DisplayName.SetDefault("");
			Main.npcFrameCount[NPC.type] = 29;//npc动画帧数量
		}
		public override void SetDefaults()
		{
			NPC.aiStyle = -1;
			NPC.width = 308;
			NPC.height = 380;
			NPC.defense = 8;
			NPC.damage = 45;
			NPC.lifeMax = 2200;

			NPC.value = 0;
			NPC.knockBackResist = 0f;//击退抗性0代表100%
			NPC.noGravity = true;
			NPC.Hitbox = new Rectangle(NPC.Hitbox.X, NPC.Hitbox.Y, NPC.Hitbox.Width - 30, NPC.Hitbox.Height - 60);
			DrawOffsetY = 15;
			DrawOffsetY = 12;
		}

        public override void FindFrame(int frameHeight)
        {
			NPC.frameCounter++;
			if (NPC.frameCounter < 10)
			{
				NPC.frame.Y = 28 * frameHeight;
			}
			else if (NPC.frameCounter < 20)
			{
				NPC.frame.Y = 27 * frameHeight;
			}
			else if (NPC.frameCounter < 30)
			{
				NPC.frame.Y = 26 * frameHeight;
			}
			else if (NPC.frameCounter < 40)
			{
				NPC.frame.Y = 25 * frameHeight;
			}
			else if (NPC.frameCounter < 50)
			{
				if (NPC.frameCounter == 40) NPC.Center = new Vector2(NPC.ai[0], NPC.ai[1]);
				NPC.frame.Y = 24 * frameHeight;
			}
			else if (NPC.frameCounter < 60)
			{
				NPC.frame.Y = 25 * frameHeight;
			}
			else if (NPC.frameCounter < 70)
			{
				NPC.frame.Y = 26 * frameHeight;
			}
			else if (NPC.frameCounter < 80)
			{
				NPC.frame.Y = 27 * frameHeight;
			}
			else if (NPC.frameCounter < 90)
			{
				NPC.frame.Y = 28 * frameHeight;
			}
			else 
			{
				NPC.active = false;
			}
		}

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            return false;
        }

        public override bool CanHitNPC(NPC target)/* tModPorter Suggestion: Return true instead of null */
        {
            return false;
        }

        public override bool? CanBeHitByItem(Player player, Item item)
        {
            return false;
        }

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            return false;
        }
    }
	/// <summary>
	/// 召唤后出现在玩家正上方，周围顺时针环绕着充能晶体（8个）充能晶体在旋转过程中会不断释放激光，
	/// 释放过程中地震者缓慢上浮，释放5/7/8次后晶体会自动爆炸并发射一圈激光，激光数量为12，地震者迅速下砸（开头杀）
	/// </summary>
	class SpawnState : NPCState
    {
		private int[] projarr = new int[8];
		private int ShootTime = 50;
		public override void AI(NPCStateMachine n)
        {
			NPC = n.NPC;
			n.Timer++;
			if (n.Timer == 1)
			{
				for (int i = 0; i < projarr.Length; i++)
				{
					projarr[i] = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero,
						ModContent.ProjectileType<ChargedCrystalProjectile>(), EmptySetUtils.ScaledProjDamage(((EarthShaker)NPC.ModNPC).laserDamage), 0, NPC.target);
				}
				NPC.velocity = new(0, -0.2f);
			}
			else if (n.Count < ((EarthShaker)NPC.ModNPC).laserCount)
			{
				//充能水晶旋转
				for (int i = 0; i < projarr.Length; i++)
				{
					if (Main.projectile[projarr[i]].active && Main.projectile[projarr[i]].type == ModContent.ProjectileType<ChargedCrystalProjectile>())
					{
						var t = n.Timer * 0.075f;
						//projarr[i].velocity = Vector2.Zero;// 要把弹幕速度归零，否则圆会有一个位移
						Main.projectile[projarr[i]].Center = NPC.Center + new Vector2((float)Math.Cos(t + (i + 1) * 3), (float)Math.Sin(t + (i + 1) * 3)) * 200f; // 半径r = 130，以玩家中心为圆心  //1 一排 2 三等分 3 2等分
						Main.projectile[projarr[i]].timeLeft = 2;
					}
				}
				//发射激光
				if (n.Timer % ShootTime == 0)
				{
					for (int i = 0; i < projarr.Length; i++)
					{
						if (Main.projectile[projarr[i]].active && Main.projectile[projarr[i]].type == ModContent.ProjectileType<ChargedCrystalProjectile>())
						{
							Projectile.NewProjectile(Main.projectile[projarr[i]].GetSource_FromAI(), Main.projectile[projarr[i]].position,
								(n.target.Center - Main.projectile[projarr[i]].Center).SafeNormalize(Vector2.UnitX) * 24f,
								ModContent.ProjectileType<激光弹幕>(), EmptySetUtils.ScaledProjDamage(((EarthShaker)NPC.ModNPC).laserDamage), 0f, NPC.target);
							SoundEngine.PlaySound(SoundID.Item33, NPC.position); //激光声
						}
					}
					n.Count++;
				}

				//跟随玩家并悬浮在头上 待优化加阻尼和最大X速度限制！！！
				if (NPC.Center.X > n.target.Center.X)
				{
					if (NPC.velocity.X >= -6)
					{
						NPC.velocity.X -= 0.2f;
					}
				}
				else if (NPC.Center.X < n.target.Center.X)
				{
					if (NPC.velocity.X <= 6)
					{
						NPC.velocity.X += 0.2f;
					}
				}
			}
			else if (n.Timer == (n.Count + 1) * ShootTime)
			{
				//下砸
				NPC.velocity.Y = 20f;
				NPC.velocity.X = 0f;
			}
			else if (n.Timer > (n.Count + 1) * ShootTime) 
			{
				if (NPC.velocity.Y == 0)
				{
					SoundEngine.PlaySound(SoundID.Item14, NPC.position);
					n.Timer = 0;
					n.Count = 0;
					n.SetState<NormalState>();
				}
			}
		}
		public override void FindFrame(NPCStateMachine n, int frameHeight)
		{
			NPC = n.NPC;
			NPC.frameCounter++;
			if (NPC.frameCounter < 10)
			{
				NPC.frame.Y = 0 * frameHeight;
			}
			else if (NPC.frameCounter < 20)
			{
				NPC.frame.Y = 1 * frameHeight;
			}
			else if (NPC.frameCounter < 30)
			{
				NPC.frame.Y = 2 * frameHeight;
			}
			else if (NPC.frameCounter < 40)
			{
				NPC.frame.Y = 3 * frameHeight;
			}
			else
			{
				NPC.frameCounter = 0;
				NPC.frame.Y = 0 * frameHeight;
			}
		}
	}
	/// <summary>
	/// npc状态：空闲
	/// </summary>
	class NormalState : NPCState
	{
		//不攻击时会停在原地2.5秒，造成接触伤害
		public override void AI(NPCStateMachine n)
		{
			NPC = n.NPC;
			n.Timer++;
			if (n.Timer == 1)
			{
				NPC.velocity = Vector2.Zero;
			}
			else if (n.Timer > 2.5 * 60)
			{
				n.Timer = 0;
				//当npc血量低于50%时,狂暴攻击状态
				if (NPC.life <= NPC.lifeMax / 2)
				{
					n.SetState<RageAttackState>();
				}
				else 
				{
					n.SetState<AttackState>();
				}
			}
			//当npc血量低于50%时,允许特殊状态
			if (NPC.life <= NPC.lifeMax / 2) n.allowNPCStates1AI = true;
			//无存活玩家，npc逃跑
			if (!n.targetIsActive) n.SetState<RunState>();
		}
		public override void FindFrame(NPCStateMachine n, int frameHeight) 
		{
			NPC = n.NPC;
			NPC.frameCounter++;
			if (NPC.frameCounter < 10)
			{
				NPC.frame.Y = 0 * frameHeight;
			}
			else if (NPC.frameCounter < 20)
			{
				NPC.frame.Y = 1 * frameHeight;
			}
			else if (NPC.frameCounter < 30)
			{
				NPC.frame.Y = 2 * frameHeight;
			}
			else if (NPC.frameCounter < 40)
			{
				NPC.frame.Y = 3 * frameHeight;
			}
			else
			{
				NPC.frameCounter = 0;
				NPC.frame.Y = 0 * frameHeight;
			}
		}
	}
	/// <summary>
	/// npc状态：攻击
	/// </summary>
	class AttackState : NPCState
	{
		public override void AI(NPCStateMachine n)
		{
			NPC = n.NPC;
			n.Timer++;
			if (n.Timer == 1)
			{
				switch (Main.rand.Next(1, 4)) 
				{
					case 1:
						n.SetState<AttackState1>();
						break;
					case 2:
						n.SetState<AttackState2>();
						break;
					case 3:
						n.SetState<AttackState3>();
						break;
				}
				n.allowNPCAttackStatesAI = true;
			}
		}
	}
	/// <summary>
	/// npc状态：残血攻击
	/// </summary>
	class RageAttackState : NPCState
	{
		public override void AI(NPCStateMachine n)
		{
			NPC = n.NPC;
			n.Timer++;
			if (n.Timer == 1)
			{
				switch (Main.rand.Next(1, 4))
				{
					case 1:
						n.SetState<AttackState2>();
						break;
					case 2:
						n.SetState<AttackState3>();
						break;
					case 3:
						n.SetState<AttackState4>();
						break;
				}
				n.allowNPCAttackStatesAI = true;
			}
		}
	}
	/// <summary>
	/// npc状态：逃跑
	/// </summary>
	class RunState : NPCState
	{
		public override void AI(NPCStateMachine n)
		{
			//禁用所有ai
			n.allowNPCStatesAI = false;
			n.allowNPCAttackStatesAI = false;
			n.allowNPCStates1AI = false;
			n.allowNPCStates2AI = false;
			NPC.velocity.Y = 10f;
			NPC.EncourageDespawn(10);
			NPC.noTileCollide = true;
		}
		public override void FindFrame(NPCStateMachine n, int frameHeight)
		{
			NPC = n.NPC;
			NPC.frameCounter++;
			if (NPC.frameCounter < 10)
			{
				NPC.frame.Y = 0 * frameHeight;
			}
			else if (NPC.frameCounter < 20)
			{
				NPC.frame.Y = 1 * frameHeight;
			}
			else if (NPC.frameCounter < 30)
			{
				NPC.frame.Y = 2 * frameHeight;
			}
			else if (NPC.frameCounter < 40)
			{
				NPC.frame.Y = 3 * frameHeight;
			}
			else
			{
				NPC.frameCounter = 0;
				NPC.frame.Y = 0 * frameHeight;
			}
		}
	}
	/// <summary>
	/// 攻击方式1:憾地进行一次特大跳，下落时会加快速度，其落点必定是开始下落时玩家所处的位置，
	/// 每次落地是会在落点生成一个小冲击波（冲击波处会生成可以减速玩家的雾气），连续跳3次
	/// 二阶段换为AttackState4
	/// </summary>
	class AttackState1 : NPCAttackState
    {
        public override void AI(NPCStateMachine n)
        {
			NPC = n.NPC;
			n.AttackTimer++;

			if (n.NPCAttackStatesCount < 3)
			{
				((EarthShaker)NPC.ModNPC).BigJump(n);
			}
			else
			{
				n.AttackTimer = 0;
				n.Timer = 0;
				n.SetState<NormalState>();
				n.NPCAttackStatesCount = 0;
				n.allowNPCAttackStatesAI = false;
			}
		}
		public override void FindFrame(NPCStateMachine n, int frameHeight)
		{
			NPC = n.NPC;
			NPC.frameCounter++;

			if (NPC.velocity.Y <= 0)
			{
				if (NPC.frameCounter < 10)
				{
					NPC.frame.Y = 4 * frameHeight;
				}
				else if (NPC.frameCounter < 20)
				{
					NPC.frame.Y = 5 * frameHeight;
				}
				else if (NPC.frameCounter < 30)
				{
					NPC.frame.Y = 6 * frameHeight;
				}
				else if (NPC.frameCounter < 40)
				{
					NPC.frame.Y = 7 * frameHeight;
				}
				else if (NPC.frameCounter < 50)
				{
					NPC.frame.Y = 8 * frameHeight;
				}
				else
				{
					NPC.frameCounter = 0;
					NPC.frame.Y = 4 * frameHeight;
				}
			}
			else 
			{
				if (NPC.frameCounter < 10)
				{
					NPC.frame.Y = 9 * frameHeight;
				}
				else if (NPC.frameCounter < 20)
				{
					NPC.frame.Y = 10 * frameHeight;
				}
				else if (NPC.frameCounter < 30)
				{
					NPC.frame.Y = 11 * frameHeight;
				}
				else if (NPC.frameCounter < 40)
				{
					NPC.frame.Y = 12 * frameHeight;
				}
				else if (NPC.frameCounter < 50)
				{
					NPC.frame.Y = 13 * frameHeight;
				}
				else if (NPC.frameCounter < 60)
				{
					NPC.frame.Y = 14 * frameHeight;
				}
				else if (NPC.frameCounter < 70)
				{
					NPC.frame.Y = 15 * frameHeight;
				}
				else
				{
					NPC.frameCounter = 0;
					NPC.frame.Y = 9 * frameHeight;
				}
			}

		}
	}
	/// <summary>
	/// 攻击方式2:憾地在玩家头上生成一个准心，跟随玩家移动,自身瞬移到玩家正上方40格图格处（在瞬移帧的第6帧时瞬移），
	/// 播放出现帧时准心停止移动，达到并播放完瞬移出现帧时立即快速下落，此时他会穿透除准心下面的所有物块，
	/// 落地时向两边升起1排大冲击波，大冲击波会移动35个图格（一个冲击波宽5个图格），击中后对玩家造成5秒碎甲buff
	/// </summary>
	class AttackState2 : NPCAttackState
	{
        private Vector2 telePos;
        int attackAimedIndex;
        private int fakeNPCIndex;
        Vector2 pos;
		public override void AI(NPCStateMachine n)
		{
			NPC = n.NPC;
			n.AttackTimer++;
			if (n.AttackTimer == 1)
			{
				telePos = n.target.Center + new Vector2(0, -40 * 16);
				//生成准心和替身
				attackAimedIndex = Projectile.NewProjectile(NPC.GetSource_FromAI(), n.target.Center + new Vector2(0, -30), Vector2.Zero, ModContent.ProjectileType<AttackAimedProjectile>(),0,0, Main.myPlayer , n.target.whoAmI, 1);
				fakeNPCIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)(telePos.X), (int)(telePos.Y+NPC.height/2), ModContent.NPCType<FakeNPC>(),ai0:NPC.Center.X,ai1:NPC.Center.Y);
			}
			else if (n.AttackTimer == 10)
			{
			}
			else if (n.AttackTimer == 60)
			{
				//互换位置，准心停止跟随
				//NPC.Center = n.target.Center + new Vector2(0, -40 * 16);
				//Main.projectile[attackAimedIndex].ai[1] = 1;
				pos = Main.projectile[attackAimedIndex].Center;
			}
			else if (n.AttackTimer == 150)
			{
				//下砸，消除准心
				NPC.velocity = new(0, 20);
				Main.projectile[attackAimedIndex].Kill();
				NPC.noTileCollide = true;
				NPC.frameCounter = 0;
			}
			else if (n.AttackTimer > 150) 
			{
				NPC.noTileCollide = NPC.Center.Y + 170 < pos.Y ? true : false;
				if (NPC.velocity.Y == 0)
				{
					//落地
					//向两边升起1排大冲击波，大冲击波会移动35个图格（一个冲击波宽5个图格）
					SoundEngine.PlaySound(SoundID.Item14, NPC.position);

					Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + new Vector2(-NPC.width / 2, NPC.height/2-80), Vector2.Zero, ModContent.ProjectileType<BigShockWaveProjectile>(), EmptySetUtils.ScaledProjDamage(((EarthShaker)NPC.ModNPC).bigShockWaveDamage), 0, Main.myPlayer, 1);
					Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + new Vector2(NPC.width / 2, NPC.height/2-80), Vector2.Zero, ModContent.ProjectileType<BigShockWaveProjectile>(), EmptySetUtils.ScaledProjDamage(((EarthShaker)NPC.ModNPC).bigShockWaveDamage), 0, Main.myPlayer, 2);
					NPC.noTileCollide = true;
					//切换状态
					n.AttackTimer = 0;
					n.Timer = 0;
					n.SetState<NormalState>();
					n.NPCAttackStatesCount = 0;
					n.allowNPCAttackStatesAI = false;
				}
			
			}
		}
		public override void FindFrame(NPCStateMachine n, int frameHeight)
		{
			NPC = n.NPC;

			NPC.frameCounter++;
			if (NPC.velocity.Y > 0)
			{
				if (NPC.frameCounter < 10)
				{
					NPC.frame.Y = 9 * frameHeight;
				}
				else if (NPC.frameCounter < 20)
				{
					NPC.frame.Y = 10 * frameHeight;
				}
				else if (NPC.frameCounter < 30)
				{
					NPC.frame.Y = 11 * frameHeight;
				}
				else if (NPC.frameCounter < 40)
				{
					NPC.frame.Y = 12 * frameHeight;
				}
				else if (NPC.frameCounter < 50)
				{
					NPC.frame.Y = 13 * frameHeight;
				}
				else if (NPC.frameCounter < 60)
				{
					NPC.frame.Y = 14 * frameHeight;
				}
				else if (NPC.frameCounter < 70)
				{
					NPC.frame.Y = 15 * frameHeight;
				}
				else
				{
					NPC.frameCounter = 0;
					NPC.frame.Y = 9 * frameHeight;
				}
			}
			else 
			{
				if (NPC.frameCounter < 10)
				{
					NPC.frame.Y = 0 * frameHeight;
				}
				else if (NPC.frameCounter < 20)
				{
					NPC.frame.Y = 21 * frameHeight;
				}
				else if (NPC.frameCounter < 30)
				{
					NPC.frame.Y = 22 * frameHeight;
				}
				else if (NPC.frameCounter < 40)
				{
					NPC.frame.Y = 23 * frameHeight;
				}
				else if (NPC.frameCounter < 50)
				{
					if (NPC.frameCounter == 40) NPC.Center = telePos;
					NPC.frame.Y = 24 * frameHeight;
				}
				else if (NPC.frameCounter < 60)
				{
					NPC.frame.Y = 23 * frameHeight;
				}
				else if (NPC.frameCounter < 70)
				{
					NPC.frame.Y = 22 * frameHeight;
				}
				else if (NPC.frameCounter < 80)
				{
					NPC.frame.Y = 21 * frameHeight;
				}
				else 
				{
					NPC.frame.Y = 0 * frameHeight;
				}
			}
		}
	}
	/// <summary>
	/// 憾地停在原地，分别从两个脚那发射两个导弹，导弹生成后会向上飞行，飞行2秒后加速飞向玩家当前位置（不会跟踪）,导弹不穿墙，击中物块或玩家后会产生一场范围极小的爆炸
	/// </summary>
	class AttackState3 : NPCAttackState
	{
		public override void AI(NPCStateMachine n)
		{
			NPC = n.NPC;
			n.AttackTimer++;
			if (n.AttackTimer == 50)
			{
				//发射导弹
				Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + new Vector2(-80,50), Vector2.Zero, ModContent.ProjectileType<钻地导弹>(), EmptySetUtils.ScaledProjDamage(((EarthShaker)NPC.ModNPC).missileDamage),0,Main.myPlayer, n.target.whoAmI, 0);
				Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + new Vector2(80, 50), Vector2.Zero, ModContent.ProjectileType<钻地导弹>(), EmptySetUtils.ScaledProjDamage(((EarthShaker)NPC.ModNPC).missileDamage), 0, Main.myPlayer, n.target.whoAmI, 0);
				//Main.NewText("发射导弹，嗖");
			}
			else if (n.AttackTimer == 130) 
			{
				n.AttackTimer = 0;
				n.Timer = 0;
				n.SetState<NormalState>();
				n.NPCAttackStatesCount = 0;
				n.allowNPCAttackStatesAI = false;
			}
		}
		public override void FindFrame(NPCStateMachine n, int frameHeight)
		{
			NPC = n.NPC;
			NPC.frameCounter++;
			if (NPC.frameCounter < 10)
			{
				NPC.frame.Y = 16 * frameHeight;
			}
			else if (NPC.frameCounter < 20)
			{
				NPC.frame.Y = 17 * frameHeight;
			}
			else if (NPC.frameCounter < 30)
			{
				NPC.frame.Y = 18 * frameHeight;
			}
			else if (NPC.frameCounter < 40)
			{
				NPC.frame.Y = 19 * frameHeight;
			}
			else if (NPC.frameCounter < 50)
			{
				NPC.frame.Y = 20 * frameHeight;
			}
			else if (NPC.frameCounter < 60)
			{
				NPC.frame.Y = 19 * frameHeight;
			}
			else if (NPC.frameCounter < 70)
			{
				NPC.frame.Y = 18 * frameHeight;
			}
			else if (NPC.frameCounter < 80)
			{
				NPC.frame.Y = 17 * frameHeight;
			}
			else if (NPC.frameCounter < 90)
			{
				NPC.frame.Y = 16 * frameHeight;
			}
			else 
			{
				NPC.frame.Y = 0 * frameHeight;
			}
		}
	}
	/// <summary>
	/// 二阶段（血量剩50%时）
	/// 特大跳的次数变成5次，特大跳后会以极快的速度移动到玩家上方25格物块处，达到后停滞30帧，
	/// 然后快速下落，重复两次，每次结束后会在原地停留60帧，然后继续移动（也有小冲击波）
	/// </summary>
	class AttackState4 : NPCAttackState
	{
		public override void AI(NPCStateMachine n)
		{
			NPC = n.NPC;
			n.AttackTimer++;
			if (n.NPCAttackStatesCount < 5)
			{
				((EarthShaker)NPC.ModNPC).BigJump(n);
			}
			else 
			{
				n.AttackTimer = 0;
				n.Timer = 0;
				n.SetState<NormalState>();
				n.NPCAttackStatesCount = 0;
				n.allowNPCAttackStatesAI = false;
			}
		}
	}
	/// <summary>
	/// 二阶段（血量剩50%时）
	/// 每隔3秒就会从头上那个灯那里向玩家位置发射速度较快的不穿墙的充能晶体弹幕，晶体自身会以一个较快的速度旋转，会发光，有淡蓝色的会发光的粒子拖尾特效
	/// </summary>
	class AttackState5 : NPCState1
	{
		int timer = 0;
		public override void AI(NPCStateMachine n)
		{
			NPC = n.NPC;
			timer++;
			if (timer == 180) 
			{
				Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + new Vector2(0, -NPC.height / 2 -40), (n.target.Center - NPC.Center + new Vector2(0, NPC.height / 2 - 40)).SafeNormalize(Vector2.UnitX)*10, ModContent.ProjectileType<ChargedCrystal2Projectile>(), EmptySetUtils.ScaledProjDamage(((EarthShaker)NPC.ModNPC).halfLifeChargedCrystalDamage), 0, Main.myPlayer);
				timer = 0;
			}
		}
	}
}