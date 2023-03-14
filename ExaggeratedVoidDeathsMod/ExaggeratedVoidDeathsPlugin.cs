using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Xan.ROR2VoidPlayerCharacterCommon;
using Xan.ROR2VoidPlayerCharacterCommon.DamageBehavior;

namespace ExaggeratedVoidDeathsMod {
	[BepInDependency(R2API.R2API.PluginGUID)]
	[BepInDependency("Xan.VoidPlayerCharacterCommon")]
	[R2APISubmoduleDependency(nameof(DamageAPI))]
	[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
	[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
	public class ExaggeratedVoidDeathsPlugin : BaseUnityPlugin {
		public const string PLUGIN_GUID = PLUGIN_AUTHOR + "." + PLUGIN_NAME;
		public const string PLUGIN_AUTHOR = "Xan";
		public const string PLUGIN_NAME = "ExaggeratedVoidDeaths";
		public const string PLUGIN_VERSION = "1.0.0";
		
		public void Awake() {
			Log.Init(Logger);
			IL.RoR2.HealthComponent.TakeDamage += SetUsesVoidDeath;
			On.RoR2.HealthComponent.TakeDamage += AfterTakingDamage;
		}

		private static bool _lastDamageWasVoidDeathFromLenses = false;

		private static void SetUsesVoidDeath(ILContext il) {
			ILCursor cursor = new ILCursor(il);
			cursor.GotoNext(
				instruction => instruction.MatchLdarg(1),
				instruction => instruction.MatchDup(),
				instruction => instruction.MatchLdfld("RoR2.DamageInfo", "damageType"),
				instruction => instruction.MatchLdcI4(0x10000),
				instruction => instruction.MatchOr(),
				instruction => instruction.MatchStfld("RoR2.DamageInfo", "damageType")
			);
			cursor.EmitDelegate(SetLastDamageWasVoidDeath);
		}

		private static void SetLastDamageWasVoidDeath() {
			_lastDamageWasVoidDeathFromLenses = true;
			// This technique is, quite frankly, pretty horrifying. It wouldn't work in a multithreaded environment either.
		}

		private void AfterTakingDamage(On.RoR2.HealthComponent.orig_TakeDamage originalMethod, HealthComponent @this, DamageInfo damageInfo) {
			bool wasAliveBefore = @this.alive;
			originalMethod(@this, damageInfo);
			if (!_lastDamageWasVoidDeathFromLenses && !@this.alive && wasAliveBefore) {
				if (XanVoidAPI.ShouldShowVoidDeath(damageInfo)) {
					EffectManager.SpawnEffect(
						VoidEffects.SilentVoidCritDeathEffect,
						new EffectData {
							origin = @this.body.corePosition,
							scale = @this.body ? @this.body.radius : 1f
						},
						true
					);
				}
			}
		}
	}
}
